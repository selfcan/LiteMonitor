using System;
using System.Drawing;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class ThresholdPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        private class ThresholdInputs { public LiteUnderlineInput Warn; public LiteUnderlineInput Crit; }

        private ThresholdInputs _inCpuLoad;
        private ThresholdInputs _inCpuTemp;
        private ThresholdInputs _inDisk;
        private ThresholdInputs _inNetUp;
        private ThresholdInputs _inNetDown;
        private ThresholdInputs _inDataUp;
        private ThresholdInputs _inDataDown;

        public ThresholdPage()
        {
            this.BackColor = UIColors.MainBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            _container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            this.Controls.Add(_container);
        }

        public override void OnShow()
        {
            if (Config == null || _isLoaded) return;
            _container.SuspendLayout();
            _container.Controls.Clear();

            // 1. 硬件负载组
            var grpHardware = new LiteSettingsGroup("Hardware Limits");
            
            _inCpuLoad = AddThresholdRow(grpHardware, "CPU / GPU Load", Config.Thresholds.Load, "%", "Warn", "Crit");
            _inCpuTemp = AddThresholdRow(grpHardware, "Temperature", Config.Thresholds.Temp, "°C", "Warn", "Crit");

            AddGroupToPage(grpHardware);

            // 2. 网络与磁盘组
            var grpNet = new LiteSettingsGroup("Network & Disk");
            
            _inDisk = AddThresholdRow(grpNet, "Disk I/O", Config.Thresholds.DiskIOMB, "MB/s", "Warn", "Crit");
            _inNetUp = AddThresholdRow(grpNet, "Upload Speed", Config.Thresholds.NetUpMB, "MB/s", "Warn", "Crit");
            _inNetDown = AddThresholdRow(grpNet, "Download Speed", Config.Thresholds.NetDownMB, "MB/s", "Warn", "Crit");

            AddGroupToPage(grpNet);

            // 3. 流量限额组
            var grpData = new LiteSettingsGroup("Daily Data Cap");
            grpData.AddFullItem(new LiteNote("Reset automatically at 00:00 every day.", 0));

            _inDataUp = AddThresholdRow(grpData, "Daily Upload", Config.Thresholds.DataUpMB, "MB", "Warn", "Limit");
            _inDataDown = AddThresholdRow(grpData, "Daily Download", Config.Thresholds.DataDownMB, "MB", "Warn", "Limit");

            AddGroupToPage(grpData);

            _container.ResumeLayout();
            _isLoaded = true;
        }

        // ★★★ 核心方法：真·单行通栏设计 ★★★
        private ThresholdInputs AddThresholdRow(
            LiteSettingsGroup group, 
            string title, 
            ValueRange val, 
            string unit,
            string labelWarn, 
            string labelCrit)
        {
            // 1. 行容器
            var panel = new Panel { 
                Height = 40, 
                Margin = new Padding(0), // 紧贴
                Padding = new Padding(0) 
            };

            // 2. 左侧标题 (普通添加，Layout中定位)
            var lblTitle = new Label {
                Text = title, AutoSize = true, 
                Font = new Font("Microsoft YaHei UI", 9F), ForeColor = UIColors.TextMain,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblTitle);

            // 3. 右侧输入框容器 (FlowPanel)
            // ★ 重点：不使用 Dock，背景透明
            var rightBox = new FlowLayoutPanel {
                AutoSize = true, 
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, 
                BackColor = Color.Transparent, // 确保透明
                Padding = new Padding(0)
            };

            // 4. 组装输入框
            // 增加宽度以容纳 "Warn" / "Critical" 等文案
            var inputWarn = new LiteUnderlineInput(val.Warn.ToString(), unit, labelWarn, 130, UIColors.TextWarn);
            inputWarn.SetTextColor(UIColors.TextWarn);

            var arrow = new Label { 
                Text = "➜", AutoSize = true, ForeColor = Color.LightGray, 
                Font = new Font("Microsoft YaHei UI", 9F), Margin = new Padding(5, 4, 5, 0) 
            };

            var inputCrit = new LiteUnderlineInput(val.Crit.ToString(), unit, labelCrit, 130, UIColors.TextCrit);
            inputCrit.SetTextColor(UIColors.TextCrit);

            rightBox.Controls.Add(inputWarn);
            rightBox.Controls.Add(arrow);
            rightBox.Controls.Add(inputCrit);

            panel.Controls.Add(rightBox);

            // 5. ★★★ 关键：使用 Layout 事件手动定位，避免遮挡底部线条 ★★★
            panel.Layout += (s, e) => {
                // 左侧垂直居中
                lblTitle.Location = new Point(0, (panel.Height - lblTitle.Height) / 2);
                
                // 右侧靠右对齐 + 垂直居中
                // 这样 rightBox 悬浮在行中间，底部留白，不会盖住底下的线
                int rightX = panel.Width - rightBox.Width;
                int rightY = (panel.Height - rightBox.Height) / 2;
                rightBox.Location = new Point(rightX, rightY);
            };

            // 6. 绘制贯穿底部的分割线
            panel.Paint += (s, e) => {
                using(var p = new Pen(Color.FromArgb(240, 240, 240))) 
                    e.Graphics.DrawLine(p, 0, panel.Height-1, panel.Width, panel.Height-1);
            };

            // 7. 添加到 Group
            group.AddFullItem(panel);

            return new ThresholdInputs { Warn = inputWarn, Crit = inputCrit };
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }

        public override void Save()
        {
            if (!_isLoaded) return;

            double Parse(LiteUnderlineInput input) 
            {
                if (double.TryParse(input.Inner.Text, out double v)) return v;
                return 0;
            }

            Config.Thresholds.Load.Warn = Parse(_inCpuLoad.Warn);
            Config.Thresholds.Load.Crit = Parse(_inCpuLoad.Crit);

            Config.Thresholds.Temp.Warn = Parse(_inCpuTemp.Warn);
            Config.Thresholds.Temp.Crit = Parse(_inCpuTemp.Crit);

            Config.Thresholds.DiskIOMB.Warn = Parse(_inDisk.Warn);
            Config.Thresholds.DiskIOMB.Crit = Parse(_inDisk.Crit);

            Config.Thresholds.NetUpMB.Warn = Parse(_inNetUp.Warn);
            Config.Thresholds.NetUpMB.Crit = Parse(_inNetUp.Crit);

            Config.Thresholds.NetDownMB.Warn = Parse(_inNetDown.Warn);
            Config.Thresholds.NetDownMB.Crit = Parse(_inNetDown.Crit);

            Config.Thresholds.DataUpMB.Warn = Parse(_inDataUp.Warn);
            Config.Thresholds.DataUpMB.Crit = Parse(_inDataUp.Crit);

            Config.Thresholds.DataDownMB.Warn = Parse(_inDataDown.Warn);
            Config.Thresholds.DataDownMB.Crit = Parse(_inDataDown.Crit);
        }
    }
}