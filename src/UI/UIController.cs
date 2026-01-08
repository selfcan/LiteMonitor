using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LiteMonitor
{
    public class UIController : IDisposable
    {
        private readonly Settings _cfg;
        private readonly Form _form;
        private readonly HardwareMonitor _mon;
        private readonly System.Windows.Forms.Timer _timer;

        private UILayout _layout;
        private bool _layoutDirty = true;
        private bool _dragging = false;

        private List<GroupLayoutInfo> _groups = new();
        private List<Column> _hxColsHorizontal = new();
        private List<Column> _hxColsTaskbar = new();
        private HorizontalLayout? _hxLayout;
        public MainForm MainForm => (MainForm)_form;

        public List<Column> GetTaskbarColumns() => _hxColsTaskbar;

        public UIController(Settings cfg, Form form)
        {
            _cfg = cfg;
            _form = form;
            _mon = new HardwareMonitor(cfg);
            _mon.OnValuesUpdated += () => _form.Invalidate();

            _layout = new UILayout(ThemeManager.Current);

            _timer = new System.Windows.Forms.Timer { Interval = Math.Max(80, _cfg.RefreshMs) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();

            ApplyTheme(_cfg.Skin);
        }

        public float GetCurrentDpiScale()
        {
            using (Graphics g = _form.CreateGraphics())
            {
                return g.DpiX / 96f;
            }
        }

        // ★★★★★ [核心修复] 解决闪烁和边距不对称 ★★★★★
        public void ApplyTheme(string name)
        {
            ThemeManager.Load(name);
            UIRenderer.ClearCache();
            var t = ThemeManager.Current;

            float dpiScale = GetCurrentDpiScale();   
            float userScale = (float)_cfg.UIScale;    
            float finalScale = dpiScale * userScale;

            t.Scale(dpiScale, userScale);

            // [修复2：边距不对称]
            // 不要设置 Width，而是设置 ClientSize。
            // 这确保了“实际绘图区域”严格等于 t.Layout.Width，消除了边框/阴影导致的右侧裁切误差。
            if (!_cfg.HorizontalMode)
            {
                t.Layout.Width = (int)(_cfg.PanelWidth * finalScale);
                // 仅设置宽度，保持高度不变(高度由 Render 决定)，或者给个初值
                _form.ClientSize = new Size(t.Layout.Width, _form.ClientSize.Height);
            }

            TaskbarRenderer.ReloadStyle(_cfg);

            _layout = new UILayout(t);
            _hxLayout = null;

            // [修复1：闪烁问题]
            // 将 BuildMetrics (耗时操作) 移到设置 BackColor 之前。
            // 这样在耗时计算期间，界面还保持旧样子，计算完后瞬间变色并重绘内容。
            BuildMetrics();
            BuildHorizontalColumns();
            _layoutDirty = true;

            // 数据准备好后，再设置背景色，紧接着立刻刷新
            _form.BackColor = ThemeManager.ParseColor(t.Color.Background);

            _timer.Interval = Math.Max(80, _cfg.RefreshMs);
            _form.Invalidate();
            _form.Update();
            UIUtils.ClearBrushCache(); 
        }

        public void RebuildLayout()
        {
            BuildMetrics();
            BuildHorizontalColumns(); 
            _layoutDirty = true;
            _form.Invalidate();
            _form.Update();
        }

        public void SetDragging(bool dragging) => _dragging = dragging;

        public void Render(Graphics g)
        {
            var t = ThemeManager.Current;
            _layout ??= new UILayout(t);

            // === 横屏模式 ===
            if (_cfg.HorizontalMode)
            {
                _hxLayout ??= new HorizontalLayout(t, _form.Width, LayoutMode.Horizontal);
                
                if (_layoutDirty)
                {
                    int h = _hxLayout.Build(_hxColsHorizontal);
                    // 同样建议横屏模式也使用 ClientSize
                    // _form.Width = ... 
                    // _form.Height = h;
                    _form.ClientSize = new Size(_hxLayout.PanelWidth, h);
                    _layoutDirty = false;
                }
                HorizontalRenderer.Render(g, t, _hxColsHorizontal, _hxLayout.PanelWidth);
                return;
            }

            // === 竖屏模式 ===
            if (_layoutDirty)
            {
                int h = _layout.Build(_groups);
                // [修复2补充] 设置高度时也使用 ClientSize，确保高度精准
                _form.ClientSize = new Size(_form.ClientSize.Width, h);
                _layoutDirty = false;
            }

            UIRenderer.Render(g, _groups, t);
        }

        private bool _busy = false;

        private async void Tick()
        {
            if (_dragging || _busy) return;
            _busy = true;

            try
            {
                await System.Threading.Tasks.Task.Run(() => _mon.UpdateAll());

                // ① 更新竖屏用的 items
                foreach (var g in _groups)
                    foreach (var it in g.Items)
                    {
                        it.Value = _mon.Get(it.Key);
                        it.TickSmooth(_cfg.AnimationSpeed);
                    }

                // ② 同步更新横版 / 任务栏用的列数据
                void UpdateCol(Column col)
                {
                    if (col.Top != null)
                    {
                        col.Top.Value = _mon.Get(col.Top.Key);
                        col.Top.TickSmooth(_cfg.AnimationSpeed);
                    }
                    if (col.Bottom != null)
                    {
                        col.Bottom.Value = _mon.Get(col.Bottom.Key);
                        col.Bottom.TickSmooth(_cfg.AnimationSpeed);
                    }
                }
                foreach (var col in _hxColsHorizontal) UpdateCol(col);
                foreach (var col in _hxColsTaskbar) UpdateCol(col);
 
                CheckTemperatureAlert();
                _form.Invalidate();   
            }
            finally
            {
                _busy = false;
            }
        }

        private void BuildMetrics()
        {
            _groups = new List<GroupLayoutInfo>();

            var activeItems = _cfg.MonitorItems
                .Where(x => x.VisibleInPanel)
                .OrderBy(x => x.SortIndex)
                .ToList();

            if (activeItems.Count == 0) return;

            string currentGroupKey = "";
            List<MetricItem> currentGroupList = new List<MetricItem>();

            foreach (var cfgItem in activeItems)
            {
                string groupKey = cfgItem.UIGroup;

                if (groupKey != currentGroupKey && currentGroupList.Count > 0)
                {
                    _groups.Add(new GroupLayoutInfo(currentGroupKey, currentGroupList));
                    currentGroupList = new List<MetricItem>();
                }

                currentGroupKey = groupKey;

                string label = LanguageManager.T("Items." + cfgItem.Key);
                var item = new MetricItem 
                { 
                    Key = cfgItem.Key, 
                    Label = label 
                };
                
                float? val = _mon.Get(item.Key);
                item.Value = val;
                if (val.HasValue) item.DisplayValue = val.Value;

                currentGroupList.Add(item);
            }

            if (currentGroupList.Count > 0)
            {
                _groups.Add(new GroupLayoutInfo(currentGroupKey, currentGroupList));
            }
        }

        private void BuildHorizontalColumns()
        {
            _hxColsHorizontal = BuildColumnsCore(forTaskbar: false);
            _hxColsTaskbar = BuildColumnsCore(forTaskbar: true);
        }

        private List<Column> BuildColumnsCore(bool forTaskbar)
        {
            var cols = new List<Column>();

            var query = _cfg.MonitorItems
                .Where(x => forTaskbar ? x.VisibleInTaskbar : x.VisibleInPanel);

            if (forTaskbar || _cfg.HorizontalFollowsTaskbar)
            {
                query = query.OrderBy(x => x.TaskbarSortIndex);
            }
            else
            {
                query = query.OrderBy(x => x.SortIndex);
            }

            var items = query.ToList();

            bool singleLine = forTaskbar && _cfg.TaskbarSingleLine;
            int step = singleLine ? 1 : 2;

            for (int i = 0; i < items.Count; i += step)
            {
                var col = new Column();
                col.Top = CreateMetric(items[i]);

                if (!singleLine && i + 1 < items.Count)
                {
                    col.Bottom = CreateMetric(items[i + 1]);
                }
                cols.Add(col);
            }

            return cols;
        }

        private MetricItem CreateMetric(MonitorItemConfig cfg)
        {
            var item = new MetricItem 
            { 
                Key = cfg.Key 
            };
            InitMetricValue(item);
            return item;
        }

        private void InitMetricValue(MetricItem? item)
        {
            if (item == null) return;
            float? val = _mon.Get(item.Key);
            item.Value = val;
            if (val.HasValue) item.DisplayValue = val.Value;
        }
        
        private void CheckTemperatureAlert()
        {
            if (!_cfg.AlertTempEnabled) return;
            if ((DateTime.Now - _cfg.LastAlertTime).TotalMinutes < 3) return;

            int globalThreshold = _cfg.AlertTempThreshold; 
            int diskThreshold = Math.Min(globalThreshold - 20, 60); 

            List<string> alertLines = new List<string>();
            string alertTitle = LanguageManager.T("Menu.AlertTemp"); 

            float? cpuTemp = _mon.Get("CPU.Temp");
            if (cpuTemp.HasValue && cpuTemp.Value >= globalThreshold)
                alertLines.Add($"CPU {alertTitle}: 🔥{cpuTemp:F0}°C");

            float? gpuTemp = _mon.Get("GPU.Temp");
            if (gpuTemp.HasValue && gpuTemp.Value >= globalThreshold)
                alertLines.Add($"GPU {alertTitle}: 🔥{gpuTemp:F0}°C");

            float? moboTemp = _mon.Get("MOBO.Temp");
            if (moboTemp.HasValue && moboTemp.Value >= globalThreshold)
                alertLines.Add($"MOBO {alertTitle}: 🔥{moboTemp:F0}°C");

            float? diskTemp = _mon.Get("DISK.Temp");
            if (diskTemp.HasValue && diskTemp.Value >= diskThreshold)
                alertLines.Add($"DISK {alertTitle}: 🔥{diskTemp:F0}°C (>{diskThreshold}°C)");

            if (alertLines.Count > 0)
            {
                string thresholdText = (alertLines.Count == 1 && alertLines[0].StartsWith("DISK")) 
                    ? $"(>{diskThreshold}°C)" 
                    : $"(>{globalThreshold}°C)";

                alertTitle += $" {thresholdText}";
                string bodyText = string.Join("\n", alertLines);
                
                ((MainForm)_form).ShowNotification(alertTitle, bodyText, ToolTipIcon.Warning);
                _cfg.LastAlertTime = DateTime.Now;
            }
        }
        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            _mon.Dispose();
        }
    }
}