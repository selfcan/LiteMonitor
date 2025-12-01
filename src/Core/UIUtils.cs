using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using System.Text.RegularExpressions;

namespace LiteMonitor.Common
{
    /// <summary>
    /// LiteMonitor 的公共 UI 工具库（所有渲染器可用）
    /// </summary>
    public static class UIUtils
    {
        // ============================================================
        // 核心：通用数值格式化 (对外入口)
        // ============================================================
        public static string FormatValue(string key, float? raw)
        {
            string k = key.ToUpperInvariant();
            float v = raw ?? 0.0f;

            // 1. 百分比类 (Load / Mem / Vram)
            if (k.Contains("LOAD") || k.Contains("VRAM") || k.Contains("MEM")) 
                return $"{v:0.0}%";

            // 2. 温度类
            if (k.Contains("TEMP")) 
                return $"{v:0.0}°C";

            // 3. 频率类 (GHz / MHz)
            if (k.Contains("CLOCK"))
                // 逻辑优化：>=1000MHz 显示 GHz，否则显示 MHz
                //return v >= 1000 ? $"{v / 1000.0:F1}GHz" : $"{v:F0}MHz";
                return $"{v / 1000.0:F1}GHz";

            // 4. 功耗类 (W)
            if (k.Contains("POWER"))
                return $"{v:F0}W";

            // 5. 流量/速率类 (NET / DISK / DATA)
            // 复用 FormatDataSize 算法
            if (k.StartsWith("NET") || k.StartsWith("DISK"))
                return FormatDataSize(v, "/s"); // 速率带 /s

            
            if (k.StartsWith("DATA"))
                return FormatDataSize(v, "");   // 总量不带 /s

            return $"{v:0.0}";
        }

        // ============================================================
        // ★★★ 核心算法：统一的字节单位换算 ★★★
        // ============================================================
        // 供 TrafficHistoryForm 和 FormatValue 共同调用
        public static string FormatDataSize(double bytes, string suffix = "")
        {
            string[] sizes = { "KB", "MB", "GB", "TB", "PB" };
            double len = bytes;
            int order = 0;
            
            // 初始就转换为 KB
            len /= 1024.0;
            
            // 自动升级单位 (>= 1024)
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024.0;
            }
  
            // 格式化细节：
            // 所有单位都保留1位小数 (如 0.1 KB, 1.2 MB)
            string format = "0.0";
            
            // 注意：不加空格(1.2MB)，为了紧凑。如果需要空格可改为 $"{len.ToString(format)} {sizes[order]}{suffix}"
            return $"{len.ToString(format)}{sizes[order]}{suffix}";
        }

        // ============================================================
        // 横屏模式专用：极简格式化 (UI 逻辑)
        // ============================================================
        public static string FormatHorizontalValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            // 1. 去掉 "/s" (省空间)
            value = value.Replace("/s", "", StringComparison.OrdinalIgnoreCase).Trim();

            // 2. 拆分解析数值和单位 ，过滤非数字+单位的字符
            var m = Regex.Match(value, @"^([\d.]+)([A-Za-z%°℃]+)$");
            if (!m.Success) return value;

            double num = double.Parse(m.Groups[1].Value);
            string unit = m.Groups[2].Value;

            // 3. 智能缩略：如果数字过大 (>=100)，去掉小数位
            // 例如: "123.4MB" -> "123MB", "99.5MB" -> "99.5MB"
            return num >= 100
                ? ((int)Math.Round(num)) + unit
                : num.ToString("0.0") + unit;
        }



        // ============================================================
        // ③ 统一颜色选择
        // ============================================================
        public static Color GetColor(string key, double value, Theme t, bool isValueText = true)
        {
            if (double.IsNaN(value)) return ThemeManager.ParseColor(t.Color.TextPrimary);
            
            // 调用核心逻辑
            int result = GetColorResult(key, value); 

            if (result == 2) return ThemeManager.ParseColor(isValueText ? t.Color.ValueCrit : t.Color.BarHigh);
            if (result == 1) return ThemeManager.ParseColor(isValueText ? t.Color.ValueWarn : t.Color.BarMid);
            return ThemeManager.ParseColor(t.Color.ValueSafe);
        }

        /// <summary>
        /// 核心：计算当前指标处于哪个报警级别 (0=Safe, 1=Warn, 2=Crit)
        /// </summary>
        public static int GetColorResult(string key, double value)
        {
            if (double.IsNaN(value)) return 0;

            string k = key.ToUpperInvariant();

            // 1. Adaptive (频率/功耗要转化成使用率数值)
            if (k.Contains("CLOCK") || k.Contains("POWER"))
            {
                value = GetAdaptivePercentage(key, value);
            }

            // 2. 使用 GetThresholds 获取阈值
            var (warn, crit) = GetThresholds(key); // GetThresholds 内部已处理 NET/DISK 分离
            
            // 3.NET/DISK 特殊处理：将 B/s 转换为 KB/s
            if (k.StartsWith("NET") || k.StartsWith("DISK") || k.Contains("DATA"))
                value /= 1024.0 * 1024.0; 

            if (value >= crit) return 2; // Crit
            if (value >= warn) return 1; // Warn
            
            return 0; // Safe
        }

        
        // ============================================================
        // ② 阈值解析（各类指标）
        // ============================================================
        public static (double warn, double crit) GetThresholds(string key)
        {
            var cfg = Settings.Load(); 
            string k = key.ToUpperInvariant();
            var th = cfg.Thresholds;

            // Load, VRAM, Mem，CLOCK/POWER
            if (k.Contains("LOAD") || k.Contains("VRAM") || k.Contains("MEM")||k.Contains("CLOCK") || k.Contains("POWER"))
                return (th.Load.Warn, th.Load.Crit);
            
            // Temp
            if (k.Contains("TEMP"))
                return (th.Temp.Warn, th.Temp.Crit);

            // Disk R/W (共享阈值)
            if (k.StartsWith("DISK"))
                return (th.DiskIOMB.Warn, th.DiskIOMB.Crit);

            // NET Up/Down (分离阈值)
            if (k.StartsWith("NET"))
            {
                if (k.Contains("UP"))
                    return (th.NetUpMB.Warn, th.NetUpMB.Crit);
                else // NET.DOWN
                    return (th.NetDownMB.Warn, th.NetDownMB.Crit);
            }

            if (k.Contains("DATA"))
            {
                if (k.Contains("UP"))
                    return (th.DataUpMB.Warn, th.DataUpMB.Crit);
                else // DATA.DOWN
                    return (th.DataDownMB.Warn, th.DataDownMB.Crit);
            }

            return (th.Load.Warn, th.Load.Crit);
        }


        // ============================================================
        // ④ 通用图形
        // ============================================================
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRoundRect(Graphics g, Rectangle r, int radius, Color c)
        {
            using var brush = new SolidBrush(c);
            using var path = RoundRect(r, radius);
            g.FillPath(brush, path);
        }

        // ============================================================
        // ⑤ 完整进度条 (恢复最低 5% 版本)
        // ============================================================
        public static void DrawBar(Graphics g, Rectangle bar, double value, string key, Theme t)
        {
            // 1. 绘制背景槽
            using (var bgPath = RoundRect(bar, bar.Height / 2))
            {
                g.FillPath(new SolidBrush(ThemeManager.ParseColor(t.Color.BarBackground)), bgPath);
            }

            // =========================================================
            // 核心计算逻辑
            // =========================================================
            double percent;
            string colorCode;

            if (key.Contains("Clock") || key.Contains("Power"))
            {
                // --- 频率/功耗 (Value / Max) ---
                // 从 Settings 读取历史最大值作为分母
                var cfg = Settings.Load();
                float max = 1.0f;
                if (key == "CPU.Clock") max = cfg.RecordedMaxCpuClock;
                else if (key == "CPU.Power") max = cfg.RecordedMaxCpuPower;
                else if (key == "GPU.Clock") max = cfg.RecordedMaxGpuClock;
                else if (key == "GPU.Power") max = cfg.RecordedMaxGpuPower;

                if (max < 1) max = 1;
                percent = value / max;

                // 颜色策略
                if (percent >= 0.9) colorCode = t.Color.BarHigh;
                else if (percent >= 0.6) colorCode = t.Color.BarMid;
                else colorCode = t.Color.BarLow;
            }
            else
            {
                // --- 默认处理 (Value / 100) ---
                percent = value / 100.0;

                // 颜色策略 (原有阈值)
                var (warn, crit) = GetThresholds(key);
                if (value >= crit) colorCode = t.Color.BarHigh;
                else if (value >= warn) colorCode = t.Color.BarMid;
                else colorCode = t.Color.BarLow;
            }

            // ★★★ 恢复您的逻辑：限制范围在 5% ~ 100% 之间 ★★★
            // 即使 value 是 0，也显示 5% 的长度，保持视觉统一
            percent = Math.Max(0.05, Math.Min(1.0, percent));

            // 2. 绘制前景条
            int w = (int)(bar.Width * percent);
            
            // 确保至少有 2px 宽度，避免圆角绘制异常
            if (w < 2) w = 2; 

            if (w > 0)
            {
                var filled = new Rectangle(bar.X, bar.Y, w, bar.Height);
                
                // 简单防越界
                if (filled.Width > bar.Width) filled.Width = bar.Width;

                using (var fgPath = RoundRect(filled, filled.Height / 2))
                {
                    g.FillPath(new SolidBrush(ThemeManager.ParseColor(colorCode)), fgPath);
                }
            }
        }

        // ============================================================
        // 辅助：获取自适应百分比 (从 Settings 读取)
        // ============================================================
        public static double GetAdaptivePercentage(string key, double val)
        {
            var cfg = Settings.Load();
            float max = 1.0f;

            if (key == "CPU.Clock") max = cfg.RecordedMaxCpuClock;
            else if (key == "CPU.Power") max = cfg.RecordedMaxCpuPower;
            else if (key == "GPU.Clock") max = cfg.RecordedMaxGpuClock;
            else if (key == "GPU.Power") max = cfg.RecordedMaxGpuPower;

            if (max < 1) max = 1;
            double pct = val / max;
            return pct > 1.0 ? 1.0 : pct;
        }
    }
}