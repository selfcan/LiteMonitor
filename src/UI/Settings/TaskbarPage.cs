using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq; 
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class TaskbarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;
        private List<Control> _customColorInputs = new List<Control>();
        private List<Control> _customLayoutInputs = new List<Control>();
        private Control _styleCombo;
        private CheckBox _chkCustomLayout;

        public TaskbarPage()
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

            CreateGeneralGroup(); 
            CreateLayoutGroup();
            CreateColorGroup();   

            _container.ResumeLayout();
            _isLoaded = true;
        }

        public override void Save()
        {
            base.Save(); // Executes all deferred sets
            TaskbarRenderer.ReloadStyle(Config);
        }

        private void CreateGeneralGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarSettings"));

            // 1. Show Taskbar
            var chkShow = group.AddToggle(this, "Menu.TaskbarShow", 
                () => Config.ShowTaskbar, 
                v => Config.ShowTaskbar = v);
            chkShow.CheckedChanged += (s, e) => EnsureSafeVisibility(null, null, chkShow);

            // 3. Style (Bold/Regular)
            var combo = group.AddComboIndex(this, "Menu.TaskbarStyle",
                new[] { LanguageManager.T("Menu.TaskbarStyleBold"), LanguageManager.T("Menu.TaskbarStyleRegular") },
                () => (!Config.TaskbarFontBold && Math.Abs(Config.TaskbarFontSize - 9f) < 0.1f) ? 1 : 0,
                idx => {
                    if (!Config.TaskbarCustomLayout) {
                        if (idx == 1) { Config.TaskbarFontSize = 9f; Config.TaskbarFontBold = false; } // Small
                        else { Config.TaskbarFontSize = 10f; Config.TaskbarFontBold = true; } // Large
                    }
                }
            );
            _styleCombo = combo; 
            _styleCombo.Enabled = !Config.TaskbarCustomLayout;

             // 4. Single Line
            group.AddToggle(this, "Menu.TaskbarSingleLine", 
                () => Config.TaskbarSingleLine, 
                v => Config.TaskbarSingleLine = v
            );

            // 2. Click Through
            group.AddToggle(this, "Menu.ClickThrough", () => Config.TaskbarClickThrough, v => Config.TaskbarClickThrough = v);
           
            // Monitor Selection
            var screens = Screen.AllScreens;
            var screenNames = screens.Select((s, i) => 
                $"{i + 1}: {s.DeviceName.Replace(@"\\.\DISPLAY", "Display ")}{(s.Primary ? " [Main]" : "")}"
            ).ToList();
            
            screenNames.Insert(0, LanguageManager.T("Menu.Auto"));
            group.AddComboIndex(this, "Menu.TaskbarMonitor", screenNames.ToArray(), 
                () => {
                    if (string.IsNullOrEmpty(Config.TaskbarMonitorDevice)) return 0;
                    var idx = Array.FindIndex(screens, s => s.DeviceName == Config.TaskbarMonitorDevice);
                    return idx >= 0 ? idx + 1 : 0;
                },
                idx => {
                    if (idx == 0) Config.TaskbarMonitorDevice = ""; 
                    else Config.TaskbarMonitorDevice = screens[idx - 1].DeviceName;
                }
            );

            // 5. Double Click Action
            string[] actions = { 
                LanguageManager.T("Menu.ActionToggleVisible"),
                LanguageManager.T("Menu.ActionTaskMgr"), 
                LanguageManager.T("Menu.ActionSettings"),
                LanguageManager.T("Menu.ActionTrafficHistory")
            };
            group.AddComboIndex(this, "Menu.DoubleClickAction", actions,
                () => Config.TaskbarDoubleClickAction,
                idx => Config.TaskbarDoubleClickAction = idx
            );

            // 4. Align
            group.AddComboIndex(this, "Menu.TaskbarAlign",
                new[] { LanguageManager.T("Menu.TaskbarAlignRight"), LanguageManager.T("Menu.TaskbarAlignLeft") },
                () => Config.TaskbarAlignLeft ? 1 : 0,
                idx => Config.TaskbarAlignLeft = (idx == 1)
            );

            // Offset
            group.AddInt(this, "Menu.TaskbarOffset", "px", 
                () => Config.TaskbarManualOffset, 
                v => Config.TaskbarManualOffset = v
            );

            group.AddHint(LanguageManager.T("Menu.TaskbarAlignTip"));
            AddGroupToPage(group);
        }

        private void CreateLayoutGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarCustomLayout")); 
            _customLayoutInputs.Clear();

            // 1. Custom Layout Toggle
            var chk = group.AddToggle(this, "Menu.TaskbarCustomLayout", 
                () => Config.TaskbarCustomLayout, 
                v => Config.TaskbarCustomLayout = v);
            
            _chkCustomLayout = chk;
            chk.CheckedChanged += (s, e) => {
                foreach(var c in _customLayoutInputs) c.Enabled = chk.Checked;
                if (_styleCombo != null) _styleCombo.Enabled = !chk.Checked;
            };

            void AddL(Control ctrl) {
                _customLayoutInputs.Add(ctrl);
                ctrl.Enabled = Config.TaskbarCustomLayout;
            }

            // 2. Font
            var installedFonts = System.Drawing.FontFamily.Families.Select(f => f.Name).ToList();
            if (!installedFonts.Contains(Config.TaskbarFontFamily)) 
                installedFonts.Insert(0, Config.TaskbarFontFamily);

            var cbFont = group.AddCombo(this, "Menu.TaskbarFont", installedFonts, 
                () => Config.TaskbarFontFamily, 
                v => Config.TaskbarFontFamily = v
            );
            AddL(cbFont);

            group.AddHint(LanguageManager.T("Menu.TaskbarCustomLayoutTip"));

            // 3. Size
            var nbSize = group.AddDouble(this, "Menu.TaskbarFontSize", "pt", 
                () => Config.TaskbarFontSize, 
                v => Config.TaskbarFontSize = (float)v
            );
            AddL(nbSize);

            // 4. Bold
            var chkBold = group.AddToggle(this, "Menu.TaskbarFontBold", 
                () => Config.TaskbarFontBold, 
                v => { 
                    if (Config.TaskbarCustomLayout) Config.TaskbarFontBold = v; 
                }
            );
            AddL(chkBold);

            // 5. Spacing
            var nbItemSp = group.AddInt(this, "Menu.TaskbarItemSpacing", "px", 
                () => Config.TaskbarItemSpacing, 
                v => Config.TaskbarItemSpacing = v
            );
            AddL(nbItemSp);

            var nbInnerSp = group.AddInt(this, "Menu.TaskbarInnerSpacing", "px", 
                () => Config.TaskbarInnerSpacing, 
                v => Config.TaskbarInnerSpacing = v
            );
            AddL(nbInnerSp);

            var nbVertPad = group.AddInt(this, "Menu.TaskbarVerticalPadding", "px", 
                () => Config.TaskbarVerticalPadding, 
                v => Config.TaskbarVerticalPadding = v
            );
            AddL(nbVertPad);

            AddGroupToPage(group);
        }

        private void CreateColorGroup()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.TaskbarCustomColors"));
            _customColorInputs.Clear();

            var chkColor = group.AddToggle(this, "Menu.TaskbarCustomColors", 
                () => Config.TaskbarCustomStyle, 
                v => Config.TaskbarCustomStyle = v);
            
            chkColor.CheckedChanged += (s, e) => {
                foreach(var c in _customColorInputs) c.Enabled = chkColor.Checked;
            };

            // Screen Color Picker
            var tbResult = new LiteUnderlineInput("#000000", "", "", 65, null, HorizontalAlignment.Center);
            tbResult.Padding = UIUtils.S(new Padding(0, 5, 0, 1)); 
            tbResult.Inner.ReadOnly = true; 

            var btnPick = new LiteSortBtn("🖌"); 
            btnPick.Location = new Point(UIUtils.S(70), UIUtils.S(1));

            btnPick.Click += (s, e) => {
                using (Form f = new Form { FormBorderStyle = FormBorderStyle.None, WindowState = FormWindowState.Maximized, TopMost = true, Cursor = Cursors.Cross })
                {
                    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    f.BackgroundImage = bmp;
                    f.MouseClick += (ms, me) => {
                        Color c = bmp.GetPixel(me.X, me.Y);
                        string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                        tbResult.Inner.Text = hex;
                        f.Close();
                        
                        string confirmMsg = string.Format("{0} {1}?", LanguageManager.T("Menu.ScreenColorPickerTip"), hex);
                        if (MessageBox.Show(confirmMsg, "LiteMonitor", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Config.TaskbarColorBg = hex;
                            foreach (var control in _customColorInputs)
                            {
                                if (control is LiteColorInput ci && ci.Input.Inner.Tag?.ToString() == "Menu.BackgroundColor")
                                {
                                    ci.HexValue = hex; 
                                    break;
                                }
                            }
                        }
                    };
                    f.ShowDialog();
                }
            };

            Panel toolCtrl = new Panel { Size = new Size(UIUtils.S(96), UIUtils.S(26)) };
            toolCtrl.Controls.Add(tbResult);
            toolCtrl.Controls.Add(btnPick);
            
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.ScreenColorPicker"), toolCtrl));

            group.AddHint(LanguageManager.T("Menu.TaskbarCustomTip"));

            void AddC(string key, Func<string> get, Action<string> set)
            {
                var c = group.AddColor(this, key, get, set);
                c.Input.Inner.Tag = key; // Tag for Picker lookup
                c.Enabled = Config.TaskbarCustomStyle;
                _customColorInputs.Add(c);
            }

            AddC("Menu.BackgroundColor", () => Config.TaskbarColorBg, v => Config.TaskbarColorBg = v);
            AddC("Menu.LabelColor", () => Config.TaskbarColorLabel, v => Config.TaskbarColorLabel = v);
            AddC("Menu.ValueSafeColor", () => Config.TaskbarColorSafe, v => Config.TaskbarColorSafe = v);
            AddC("Menu.ValueWarnColor", () => Config.TaskbarColorWarn, v => Config.TaskbarColorWarn = v);
            AddC("Menu.ValueCritColor", () => Config.TaskbarColorCrit, v => Config.TaskbarColorCrit = v);

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
