# J-Center-Lite

J-Center-Lite is an independent WinUI 3 reimplementation of the SEGOTEP / horiemtech **J-Center** control panel for QiaoSiBo 480×480 Android display modules and PC cooling hardware.

> 独立实现、不附带厂商二进制。原程序只用于行为与协议研究；仓库不包含原程序的代码、驱动、字体、图片、视频或固件。

## 能做什么

- **硬件监控**：CPU/GPU 温度、占用、频率、功耗，内存、系统盘、网络上下行、风扇/水泵转速（LibreHardwareMonitor 驱动传感器）。
- **副屏推送**：每 100 ms 把传感器数据推送到 480×480 屏幕（含 CPU/GPU/内存/硬盘/网络/风扇等 34 个字段）。
- **屏幕控制**：亮度、循环/随机/关闭播放、开关屏、重启副屏、查询终端版本。
- **主题系统**：导入 PNG/JPG/GIF/MP4/WebM 背景；可视化 480×480 主题编辑器（时间/日期/温度/占用/内存/硬盘/网络等元素，可拖拽、旋转、缩放、改色、改字号）；一键推送到设备。
- **托盘与开机**：最小化/关闭到托盘、开机自启。
- **更新**：GitHub Releases 检查；发布包可直接放到 GitHub Release。
- **ADB 桥接**：自动查找 adb.exe，自动选空闲本地端口并建立到 `22222` 的转发；OTA 包可推送到 `/sdcard/ota_package/`。
- **占用检测与接管**：副屏固件只接受一个控制连接；如果原 J-Center 仍在运行，应用会明确提示，并在你确认后结束 `WFanManager.exe` 完成接管。

## 快速开始

1. 安装 Windows 11、[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 与 Visual Studio 2022（17.12+，含 Windows App SDK C# 桌面开发工作负载）。
2. 准备 `adb.exe`（Android platform-tools）。J-Center-Lite **不捆绑** adb；在“设置 → adb.exe 路径”中选择即可。
3. 用管理员身份运行 J-Center-Lite（LibreHardwareMonitor 读取 MSR/SuperIO 通常需要管理员权限；普通权限下仍可使用内存、磁盘、网络与部分 GPU 传感器）。
4. 连接 QiaoSiBo 屏幕模组，点击“连接设备”。设备出现后，点击“开始推送到副屏”。

> 副屏固件只接受一个控制连接。若原 J-Center 仍在运行，点击“连接设备”后会弹出“需要接管副屏”对话框；确认后 J-Center-Lite 会结束原 `WFanManager.exe` 并接管。若不想接管，请先手动退出原程序。

## 构建

```powershell
# 本机若已安装 .NET SDK 8，直接：
dotnet build J-Center-Lite.sln -c Release -p:Platform=x64

# 运行协议单测
dotnet test tests/JCenterLite.Core.Tests/JCenterLite.Core.Tests.csproj -c Release

# 生成可分发的自包含包
dotnet publish src/JCenterLite.App/JCenterLite.App.csproj -c Release -r win-x64 -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None -o dist/win-x64
```

> 本机实测：`dotnet` 在 `C:\Users\7950X\.dotnet\dotnet.exe`（SDK 8.0.425），VS2022 Community 17.12.1；Windows App SDK 使用 NuGet 1.6.250602001。

## 项目结构

```
J-Center-Lite/
├─ src/JCenterLite.App/          # WinUI 3 桌面应用（MainWindow / Pages / ViewModels / Tray）
├─ src/JCenterLite.Core/         # 协议、ADB、传感器、主题、设置、更新（无 UI 依赖）
├─ tests/JCenterLite.Core.Tests/ # xUnit 协议与模型测试
├─ docs/                         # 架构、构建、发布、合规、逆向协议文档
└─ .github/workflows/ci.yml      # Windows CI：还原 + 构建 + 测试 + 发布产物
```

## 迁移原 J-Center 主题

应用内：**设置 → 主题迁移 → 从原 J-Center 导入主题**。  
命令行：

```powershell
dotnet run --project tools/JCenterThemeMigrator/JCenterThemeMigrator.csproj -c Release -- `
  "C:\Program Files (x86)\horiemtech\J-Center"
```

会导入 8 个内置主题预览、空白主题、你的自定义媒体主题（MP4/GIF）以及 `Theme\*.xml` 布局主题，输出到 `%LOCALAPPDATA%\JCenterLite`。不修改原安装目录，也不把厂商素材写进仓库。详见 [docs/theme-migration.md](docs/theme-migration.md)。

## 与原程序的关系

| 原程序 | 本项目 |
|---|---|
| WPF / .NET Framework 4.7.2 | WinUI 3 / .NET 8 |
| 自带 ADB + ffmpeg + 厂商驱动 | 使用用户自备 adb；ffmpeg 可选；不捆绑任何厂商二进制 |
| 设备端 `com.hr.cpuinfoshow` | 保持兼容，通过同一 TCP 协议控制 |
| 部分资源加密 / 混淆 | 从零实现，MIT 许可 |

协议细节、命令表与验证过程见 [docs/reverse-engineering/protocol.md](docs/reverse-engineering/protocol.md)。

## 当前边界

- **单实例**、**默认主题预览图**、**云端主题下载**、**完整中英双语 UI**、**图片裁剪 UI** 尚未实现；见 [docs/feature-matrix.md](docs/feature-matrix.md)。
- 原程序并未提供风扇占空比控制界面；本项目同样以监控/推送为主，不写 EC 寄存器、不绕过硬件保护。
- 不附带内核驱动；需要完整传感器时请让 LibreHardwareMonitor 安装其签名驱动，并以管理员运行。

## 安全与合规

- 不重新分发原厂商的 `.exe/.dll/.sys`、图片、字体、视频、APK 或固件。
- `adb.exe` 属于 Google platform-tools，本项目只调用用户本机已有副本。
- LibreHardwareMonitorLib 采用 MPL-2.0；使用其驱动前请阅读其许可与安全说明。
- 详见 [docs/legal.md](docs/legal.md)。

## License

MIT. See [LICENSE](LICENSE).

---

### English summary

J-Center-Lite is a clean-room WinUI 3 reimplementation of the J-Center panel. It monitors PC sensors through LibreHardwareMonitor, drives the QiaoSiBo 480×480 display over a documented ADB-forwarded TCP protocol, includes a visual theme editor and tray integration, and ships without any vendor binaries, assets, or kernel drivers.
