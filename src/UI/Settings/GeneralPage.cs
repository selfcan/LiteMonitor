using System;
using System.Drawing;
using System.IO;
using System.Linq; 
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class GeneralPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        private LiteComboBox _cmbLang;
        private LiteCheck _chkAutoStart;
        private LiteCheck _chkTopMost;
        private LiteComboBox _cmbRefresh;
        private LiteCheck _chkAutoHide;
        private LiteCheck _chkClickThrough;
        private LiteCheck _chkClamp;
        private LiteCheck _chkHideTray;
        private LiteCheck _chkHideMain;
        private LiteCheck _chkShowTaskbar;
        private LiteComboBox _cmbNet;
        private LiteComboBox _cmbDisk;
        private string _originalLanguage;

        // 最大限制
        private LiteUnderlineInput _txtMaxCpuPower;
        private LiteUnderlineInput _txtMaxCpuClock;
        private LiteUnderlineInput _txtMaxGpuPower;
        private LiteUnderlineInput _txtMaxGpuClock;

        public GeneralPage()
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
           
            CreateBehaviorCard(); 
            CreateSystemCard();   
            CreateSourceCard();   

            _originalLanguage = Config.Language;
            _container.ResumeLayout();
            _isLoaded = true;
        }

        // ... CreateBehaviorCard 和 CreateSystemCard 保持不变 (代码省略以节省篇幅) ...
        private void CreateBehaviorCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.Behavior"));
            _chkTopMost = new LiteCheck(Config.TopMost, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TopMost"), _chkTopMost));
            _chkClickThrough = new LiteCheck(Config.ClickThrough, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ClickThrough"), _chkClickThrough));
            _chkClamp = new LiteCheck(Config.ClampToScreen, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ClampToScreen"), _chkClamp));
            _chkAutoHide = new LiteCheck(Config.AutoHide, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AutoHide"), _chkAutoHide));
            _chkHideTray = new LiteCheck(Config.HideTrayIcon, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.HideTrayIcon"), _chkHideTray));
            _chkHideMain = new LiteCheck(Config.HideMainForm, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.HideMainForm"), _chkHideMain));
            _chkShowTaskbar = new LiteCheck(Config.ShowTaskbar, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.TaskbarShow"), _chkShowTaskbar));
            _chkHideTray.CheckedChanged += (s, e) => CheckVisibilitySafe();
            _chkHideMain.CheckedChanged += (s, e) => CheckVisibilitySafe();
            _chkShowTaskbar.CheckedChanged += (s, e) => CheckVisibilitySafe();
            AddGroupToPage(group);
        }
        
        private void CreateSystemCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.SystemSettings"));
            _cmbLang = new LiteComboBox();
            string langDir = Path.Combine(AppContext.BaseDirectory, "resources/lang");
            if (Directory.Exists(langDir)) {
                foreach (var file in Directory.EnumerateFiles(langDir, "*.json")) {
                    string code = Path.GetFileNameWithoutExtension(file);
                    _cmbLang.Items.Add(code.ToUpper());
                }
            }
            string curLang = string.IsNullOrEmpty(Config.Language) ? "en" : Config.Language;
            foreach (var item in _cmbLang.Items) {
                if (item.ToString().Contains(curLang.ToUpper())) _cmbLang.SelectedItem = item;
            }
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Language"), _cmbLang));
            _chkAutoStart = new LiteCheck(Config.AutoStart, LanguageManager.T("Menu.Enable"));
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AutoStart"), _chkAutoStart));
            AddGroupToPage(group);
        }

        private void CreateSourceCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.HardwareSettings"));

            _cmbDisk = new LiteComboBox();
            foreach (var d in HardwareMonitor.ListAllDisks()) _cmbDisk.Items.Add(d);
            SetComboVal(_cmbDisk, string.IsNullOrEmpty(Config.PreferredDisk) ? LanguageManager.T("Menu.Auto") : Config.PreferredDisk);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.DiskSource"), _cmbDisk));

            _cmbNet = new LiteComboBox();
            foreach (var n in HardwareMonitor.ListAllNetworks()) _cmbNet.Items.Add(n);
            SetComboVal(_cmbNet, string.IsNullOrEmpty(Config.PreferredNetwork) ? LanguageManager.T("Menu.Auto") : Config.PreferredNetwork);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.NetworkSource"), _cmbNet));

            _cmbRefresh = new LiteComboBox();
            int[] rates = { 100, 200, 300, 500, 600, 700, 800, 1000, 1500, 2000, 3000 };
            foreach (var r in rates) _cmbRefresh.Items.Add(r + " ms");
            SetComboVal(_cmbRefresh, Config.RefreshMs + " ms");
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Refresh"), _cmbRefresh));

            // Max Limits
            group.AddFullItem(new LiteNote("Max Limits (For Graph Scaling)", 0));

            // ★ 适配新构造函数：text, unit, suffix="", width=80
            _txtMaxCpuPower = new LiteUnderlineInput(Config.RecordedMaxCpuPower.ToString("F0"), "W", "", 80);
            group.AddItem(new LiteSettingsItem("CPU Max Power", _txtMaxCpuPower));

            _txtMaxCpuClock = new LiteUnderlineInput(Config.RecordedMaxCpuClock.ToString("F0"), "MHz", "", 80);
            group.AddItem(new LiteSettingsItem("CPU Max Clock", _txtMaxCpuClock));

            _txtMaxGpuPower = new LiteUnderlineInput(Config.RecordedMaxGpuPower.ToString("F0"), "W", "", 80);
            group.AddItem(new LiteSettingsItem("GPU Max Power", _txtMaxGpuPower));

            _txtMaxGpuClock = new LiteUnderlineInput(Config.RecordedMaxGpuClock.ToString("F0"), "MHz", "", 80);
            group.AddItem(new LiteSettingsItem("GPU Max Clock", _txtMaxGpuClock));

            AddGroupToPage(group);
        }

        // ... 辅助方法保持不变 (AddGroupToPage, Save 等) ...
        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }
        private void SetComboVal(LiteComboBox cmb, string val) { if (!cmb.Items.Contains(val)) cmb.Items.Insert(0, val); cmb.SelectedItem = val; }
        private void CheckVisibilitySafe() {
            if (!_chkShowTaskbar.Checked && _chkHideMain.Checked && _chkHideTray.Checked) {
                if (_chkHideMain.Focused) _chkHideMain.Checked = false;
                else _chkHideTray.Checked = false;
            }
        }
        private int ParseInt(string s) { string clean = new string(s.Where(char.IsDigit).ToArray()); return int.TryParse(clean, out int v) ? v : 0; }
        private float ParseFloat(string s) { string clean = new string(s.Where(c => char.IsDigit(c) || c == '.').ToArray()); return float.TryParse(clean, out float v) ? v : 0f; }

        public override void Save()
        {
            if (!_isLoaded) return;
            
            Config.AutoStart = _chkAutoStart.Checked;
            Config.TopMost = _chkTopMost.Checked;
            if (_cmbLang.SelectedItem != null) {
                string s = _cmbLang.SelectedItem.ToString();
                Config.Language = (s == "Auto") ? "" : s.Split('(')[0].Trim().ToLower();
            }
            Config.AutoHide = _chkAutoHide.Checked;
            Config.ClickThrough = _chkClickThrough.Checked;
            Config.ClampToScreen = _chkClamp.Checked;
            Config.HideTrayIcon = _chkHideTray.Checked;
            Config.HideMainForm = _chkHideMain.Checked;
            Config.ShowTaskbar = _chkShowTaskbar.Checked;
            Config.RefreshMs = ParseInt(_cmbRefresh.Text);
            if (Config.RefreshMs < 50) Config.RefreshMs = 1000;
            if (_cmbDisk.SelectedItem != null) { string d = _cmbDisk.SelectedItem.ToString(); Config.PreferredDisk = (d == "Auto") ? "" : d; }
            if (_cmbNet.SelectedItem != null) { string n = _cmbNet.SelectedItem.ToString(); Config.PreferredNetwork = (n == "Auto") ? "" : n; }

            Config.RecordedMaxCpuPower = ParseFloat(_txtMaxCpuPower.Inner.Text);
            Config.RecordedMaxCpuClock = ParseFloat(_txtMaxCpuClock.Inner.Text);
            Config.RecordedMaxGpuPower = ParseFloat(_txtMaxGpuPower.Inner.Text);
            Config.RecordedMaxGpuClock = ParseFloat(_txtMaxGpuClock.Inner.Text);

            AppActions.ApplyAutoStart(Config);
            AppActions.ApplyWindowAttributes(Config, this.MainForm); 
            AppActions.ApplyVisibility(Config, this.MainForm);
            AppActions.ApplyMonitorLayout(this.UI, this.MainForm);
            if (_originalLanguage != Config.Language) {
                AppActions.ApplyLanguage(Config, this.UI, this.MainForm);
                _originalLanguage = Config.Language; 
            }
        }
    }
}