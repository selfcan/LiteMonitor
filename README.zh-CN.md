# ⚡ LiteMonitor
轻量级、可自定义的桌面硬件监控工具

---

## 🖥️ 项目简介
**LiteMonitor** 是一个基于 **.NET 8 / WinForms** 开发的桌面硬件监控软件，
采用模块化架构与 JSON 配置主题系统，支持多语言、扁平化 UI、圆角窗口、
透明度调节、动画平滑、自定义宽度、鼠标穿透、自动隐藏、
开机自启与在线更新检测。

---

## ✨ 功能特性

| 功能 | 说明 |
|------|------|
| 🌍 多语言界面 | 支持简体中文、英语、日语、韩语、法语、德语、西班牙语、俄语 |
| 🎨 自定义主题 | 使用 JSON 文件定义颜色、字体、间距、圆角，可实时切换 |
| 📊 硬件监控 | CPU、GPU、显存、内存、磁盘、网络使用率与温度监控 |
| 🪟 窗口控制 | 圆角、透明度调节、总在最前、自动隐藏、鼠标穿透 |
| 📏 面板宽度 | 右键菜单可自由调整面板宽度，立即生效 |
| 💫 动画平滑 | 可调节数值更新速度，防止跳动或突变 |
| 🧩 主题/语言即时刷新 | 切换后即时应用，无需重启 |
| 🔠 DPI 缩放 | 字体可按比例缩放，兼容高分屏显示 |
| ⚙️ 配置自动保存 | 所有菜单更改会实时写入 settings.json |
| 🚀 开机自启 | 通过计划任务方式以管理员权限启动 |
| 🔄 自动更新 | 一键检测 GitHub 最新版本 |
| ℹ️ 关于窗口 | 显示版本号、作者及项目主页信息 |

---

## 📦 安装与运行

1. 前往 [Releases 页面](https://github.com/Diorser/LiteMonitor/releases) 下载最新版压缩包  
2. 解压后运行 `LiteMonitor.exe`  
3. 程序会自动检测系统语言并加载对应语言文件

---

## 🌐 多语言支持

语言文件存放在 `/lang/` 目录中，当前支持：

| 语言 | 文件名 |
|------|--------|
| 简体中文 | `zh.json` |
| English | `en.json` |
| 日本語 | `ja.json` |
| 한국어 | `ko.json` |
| Français | `fr.json` |
| Deutsch | `de.json` |
| Español | `es.json` |
| Русский | `ru.json` |

---

## 🎨 主题系统

主题文件存放在 `/themes/` 目录中，每个主题都是独立的 JSON 文件。

示例：
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

## 🔄 自动更新机制

程序会访问以下地址检查最新版本：
```
https://raw.githubusercontent.com/Diorser/LiteMonitor/main/version.json
```

`version.json` 示例：
```json
{
  "version": "1.0.1",
  "changelog": "优化界面细节与About窗口"
}
```

---

## ⚙️ 设置文件说明（settings.json）

| 字段 | 说明 |
|------|------|
| `Skin` | 当前主题名称 |
| `PanelWidth` | 界面宽度 |
| `Opacity` | 窗口透明度 |
| `Language` | 当前语言 |
| `TopMost` | 是否总在最前 |
| `AutoStart` | 是否开机自启 |
| `AutoHide` | 是否自动隐藏 |
| `ClickThrough` | 是否启用鼠标穿透 |
| `AnimationSpeed` | 数值平滑速度 |
| `Enabled` | 各监控项开关 |

---

## 🧩 模块结构

| 模块文件 | 功能 |
|-----------|------|
| `MainForm_Transparent.cs` | 主窗体逻辑、菜单、交互控制 |
| `UIController.cs` | 主题加载、刷新逻辑 |
| `UIRenderer.cs` | 绘制进度条与文本 |
| `UILayout.cs` | 计算动态布局 |
| `ThemeManager.cs` | 加载并解析主题文件 |
| `LanguageManager.cs` | 加载语言文件 |
| `HardwareMonitor.cs` | 硬件数据采集 |
| `AutoStart.cs` | 注册计划任务开机启动 |
| `UpdateChecker.cs` | 在线更新检查 |
| `AboutForm.cs` | 关于窗口 |

---

## 🛠️ 编译说明

### 环境要求
- Windows 10 / 11  
- .NET 8 SDK  
- Visual Studio 2022 或 Rider

### 编译命令
```bash
git clone https://github.com/Diorser/LiteMonitor.git
cd LiteMonitor
dotnet build -c Release
```

生成路径：
```
/bin/Release/net8.0-windows/LiteMonitor.exe
```

---

## 📄 开源协议
本项目基于 **MIT License** 开源，可自由使用、修改与分发。

---

## 📬 联系方式
**作者**：Diorser  
**项目主页**：[https://github.com/Diorser/LiteMonitor](https://github.com/Diorser/LiteMonitor)
