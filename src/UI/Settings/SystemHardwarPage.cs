using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class SystemHardwarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;
        private string _originalLanguage;

        public SystemHardwarPage()
        {
            this.BackColor = UIColors.MainBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            _container = new BufferedPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) }; 
            this.Controls.Add(_container);
        }

        public override void OnShow()
        {
            base.OnShow();
            if (Config == null || _isLoaded) return;

            _container.SuspendLayout();
            ClearAndDispose(_container.Controls);
            _originalLanguage = Config.Language;

            CreateSourceCard();
            CreateCalibrationCard();
            CreateSystemCard();

            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateSystemCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.SystemSettings"));

           // 1. Language
            var langs = new List<string>();
            string langDir = Path.Combine(AppContext.BaseDirectory, "resources/lang");
            if (Directory.Exists(langDir))
            {
                langs.AddRange(Directory.EnumerateFiles(langDir, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f).ToUpper()));
            }
            
            group.AddCombo(this, "Menu.Language", langs,
                () => string.IsNullOrEmpty(Config.Language) 
                        ? LanguageManager.CurrentLang.ToUpper() 
                        : Config.Language.ToUpper(),
                v => Config.Language = v.ToLower()
            );

            // 2. AutoStart
            group.AddToggle(this, "Menu.AutoStart", () => Config.AutoStart, v => Config.AutoStart = v);

            // 3. Hide Tray
            var chkTray = group.AddToggle(this, "Menu.HideTrayIcon", 
                () => Config.HideTrayIcon, 
                v => Config.HideTrayIcon = v);
            chkTray.CheckedChanged += (s, e) => EnsureSafeVisibility(null, chkTray, null);

            AddGroupToPage(group);
        }

        private void CreateCalibrationCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.Calibration"));
            string suffix = " (" + LanguageManager.T("Menu.MaxLimits") + ")";

            void AddCalib(string key, string unit, Func<float> get, Action<float> set)
            {
                string title = LanguageManager.T(key) + suffix; 
                
                var input = group.AddDouble(this, "RAW_TITLE_HACK", unit, 
                    () => (int)get(),        
                    v => set((float)(int)v)
                );
                
                // Hack to update title
                if(input.Parent.Controls[0] is Label lbl) lbl.Text = title;
            }
            
            group.AddHint(LanguageManager.T("Menu.CalibrationTip"));
            
            AddCalib("Items.CPU.Power", "W",   () => Config.RecordedMaxCpuPower, v => Config.RecordedMaxCpuPower = v);
            AddCalib("Items.CPU.Clock", "MHz", () => Config.RecordedMaxCpuClock, v => Config.RecordedMaxCpuClock = v);
            AddCalib("Items.GPU.Power", "W",   () => Config.RecordedMaxGpuPower, v => Config.RecordedMaxGpuPower = v);
            AddCalib("Items.GPU.Clock", "MHz", () => Config.RecordedMaxGpuClock, v => Config.RecordedMaxGpuClock = v);

            AddCalib("Items.CPU.Fan",   "RPM", () => Config.RecordedMaxCpuFan,     v => Config.RecordedMaxCpuFan = v);
            AddCalib("Items.CPU.Pump", "RPM", () => Config.RecordedMaxCpuPump, v => Config.RecordedMaxCpuPump = v);
            AddCalib("Items.GPU.Fan",   "RPM", () => Config.RecordedMaxGpuFan,     v => Config.RecordedMaxGpuFan = v);
            AddCalib("Items.CASE.Fan",  "RPM", () => Config.RecordedMaxChassisFan, v => Config.RecordedMaxChassisFan = v);

            AddGroupToPage(group);
        }

        private void CreateSourceCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.HardwareSettings"));
            string strAuto = LanguageManager.T("Menu.Auto");
            
            // System CPU
            group.AddToggle(this, "Menu.UseWinPerCounters", () => Config.UseWinPerCounters, v => Config.UseWinPerCounters = v);
            
            // Refresh Rate
            int[] rates = { 100, 200, 300, 500, 600, 700, 800, 1000, 1500, 2000, 3000 };
            group.AddCombo(this, "Menu.Refresh", rates.Select(r => r + " ms"),
                () => Config.RefreshMs + " ms",
                v => {
                    int val = UIUtils.ParseInt(v);
                    Config.RefreshMs = val < 50 ? 1000 : val;
                }
            );
            group.AddHint(LanguageManager.T("Menu.UseWinPerCountersTip"));

            // 1. Disk Source
            var disks = HardwareMonitor.ListAllDisks();
            disks.Insert(0, strAuto);
            group.AddCombo(this, "Menu.DiskSource", disks,
                () => string.IsNullOrEmpty(Config.PreferredDisk) ? strAuto : Config.PreferredDisk,
                v => Config.PreferredDisk = (v == strAuto) ? "" : v
            );

            // 2. Network Source
            var nets = HardwareMonitor.ListAllNetworks();
            nets.Insert(0, strAuto);
            group.AddCombo(this, "Menu.NetworkSource", nets,
                () => string.IsNullOrEmpty(Config.PreferredNetwork) ? strAuto : Config.PreferredNetwork,
                v => Config.PreferredNetwork = (v == strAuto) ? "" : v
            );

            // 3. Fan Source
            var fans = HardwareMonitor.ListAllFans(); 
            fans.Insert(0, strAuto);

            group.AddCombo(this, "Items.CPU.Fan", fans,
                () => string.IsNullOrEmpty(Config.PreferredCpuFan) ? strAuto : Config.PreferredCpuFan,
                v => Config.PreferredCpuFan = (v == strAuto) ? "" : v
            );

            group.AddCombo(this, "Items.CPU.Pump", fans,
                () => string.IsNullOrEmpty(Config.PreferredCpuPump) ? strAuto : Config.PreferredCpuPump,
                v => Config.PreferredCpuPump = (v == strAuto) ? "" : v
            );

            group.AddCombo(this, "Items.CASE.Fan", fans,
                () => string.IsNullOrEmpty(Config.PreferredCaseFan) ? strAuto : Config.PreferredCaseFan,
                v => Config.PreferredCaseFan = (v == strAuto) ? "" : v
            );
           
            // 4. Mobo Temp Source
            var moboTemps = HardwareMonitor.ListAllMoboTemps();
            moboTemps.Insert(0, strAuto);
            group.AddCombo(this, "Items.MOBO.Temp", moboTemps,
                () => string.IsNullOrEmpty(Config.PreferredMoboTemp) ? strAuto : Config.PreferredMoboTemp,
                v => Config.PreferredMoboTemp = (v == strAuto) ? "" : v
            );

            AddGroupToPage(group);
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }
    }
}
