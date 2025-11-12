# ⚡ LiteMonitor
A lightweight and customizable desktop hardware monitor

---

## 🖥️ Overview
**LiteMonitor** is a Windows desktop hardware monitor built with **.NET 8 (WinForms)**.  
It supports JSON-based themes, multilingual UI, adjustable opacity,  
smooth animation, custom width, click-through transparency,  
auto-hide, auto-start, and online update checking.

---

## ✨ Features

| Feature | Description |
|----------|-------------|
| 🌍 Multilingual UI | Supports 8 languages: Chinese, English, Japanese, Korean, French, German, Spanish, Russian |
| 🎨 Customizable Themes | JSON-defined colors, fonts, spacing, and corner radius |
| 📊 Hardware Monitoring | CPU, GPU, VRAM, memory, disk, and network usage & temperature |
| 🪟 Window Control | Rounded corners, adjustable opacity, always-on-top, auto-hide, click-through |
| 📏 Adjustable Width | Change panel width instantly from right-click menu |
| 💫 Smooth Animation | Control animation speed for value transitions |
| 🧩 Live Theme/Language Refresh | Changes apply instantly without restart |
| 🔠 DPI Scaling | Fonts scale dynamically for high-DPI displays |
| ⚙️ Auto-Save Settings | All menu actions are saved immediately to settings.json |
| 🚀 Auto Start | Starts via Task Scheduler with admin privileges |
| 🔄 Auto Update | Check GitHub for the latest version |
| ℹ️ About Window | Displays version, author, and repository info |

---

## 📦 Installation

1. Download the latest version from [GitHub Releases](https://github.com/Diorser/LiteMonitor/releases)  
2. Extract and run `LiteMonitor.exe`  
3. The app automatically loads your system language

---

## 🌐 Multilingual Support

Language files are stored under `/lang/`:

| Language | File |
|-----------|------|
| Chinese (Simplified) | `zh.json` |
| English | `en.json` |
| Japanese | `ja.json` |
| Korean | `ko.json` |
| French | `fr.json` |
| German | `de.json` |
| Spanish | `es.json` |
| Russian | `ru.json` |

---

## 🎨 Theme System

Themes are stored in the `/themes/` directory as JSON files.

Example:
```json
{
  "name": "DarkFlat_Classic",
  "layout": { "rowHeight": 40, "cornerRadius": 10 },
  "color": {
    "background": "#202225",
    "textPrimary": "#EAEAEA",
    "barLow": "#00C853"
  }
}
```

---

## 🔄 Auto Update

LiteMonitor checks updates from:
```
https://raw.githubusercontent.com/Diorser/LiteMonitor/main/version.json
```

Example `version.json`:
```json
{
  "version": "1.0.1",
  "changelog": "UI improvements and About window optimization"
}
```

If a newer version is detected, it prompts the user to open the GitHub Releases page.

---

## ⚙️ Settings (settings.json)

| Field | Description |
|--------|-------------|
| `Skin` | Current theme |
| `PanelWidth` | Panel width |
| `Opacity` | Window opacity |
| `Language` | Current language |
| `TopMost` | Always on top |
| `AutoStart` | Run at startup |
| `AutoHide` | Auto-hide when near screen edge |
| `ClickThrough` | Enable mouse click-through |
| `AnimationSpeed` | Smooth animation speed |
| `Enabled` | Item visibility toggles |

---

## 🧩 Architecture

| File | Role |
|------|------|
| `MainForm_Transparent.cs` | Main form logic & UI interaction |
| `UIController.cs` | Theme and rendering controller |
| `UIRenderer.cs` | UI drawing engine |
| `UILayout.cs` | Layout calculation |
| `ThemeManager.cs` | Theme loading and font management |
| `LanguageManager.cs` | Language file handler |
| `HardwareMonitor.cs` | Hardware data collection |
| `AutoStart.cs` | Auto-start manager |
| `UpdateChecker.cs` | Online update checking |
| `AboutForm.cs` | About window |

---

## 🛠️ Build Instructions

### Requirements
- Windows 10 / 11  
- .NET 8 SDK  
- Visual Studio 2022 or JetBrains Rider

### Build
```bash
git clone https://github.com/Diorser/LiteMonitor.git
cd LiteMonitor
dotnet build -c Release
```

Output:
```
/bin/Release/net8.0-windows/LiteMonitor.exe
```

---

## 📄 License
Released under the **MIT License** — free to use, modify, and distribute.

---

## 💬 Contact
**Author:** Diorser  
**GitHub:** [https://github.com/Diorser/LiteMonitor](https://github.com/Diorser/LiteMonitor)
