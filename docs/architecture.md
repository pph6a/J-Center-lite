# 架构

```
WinUI 3 App (src/JCenterLite.App)
  MainWindow / NavigationView / Pages / ViewModels / TrayIconService
  Controls: ThemeCanvas（480×480 预览，支持图片/GIF/视频背景）/ ThemeThumbnail（列表缩略图）
  Services: ThemeThumbnailService（视频首帧抽取 + 缓存）
        │
        ├─ JCenterLite.Core (net8.0, UI-free)
        │    Protocol: JCenterFrame / Commands / TelemetryCodec / CommandBuilder / CustomThemeBuilder
        │    Services: AdbCli / JCenterDeviceClient / HardwareMonitorService / TelemetryPump
        │              SettingsService / ThemeStore / JCenterThemeMigrator / StartupService / UpdateService
        │    Models: FanTelemetry / ThemeDefinition / AppSettings / DeviceStatus / SensorReading
        │
        └─ 外部依赖：LibreHardwareMonitorLib (MPL-2.0)、adb.exe（用户提供）
```

## 数据流

1. `ShellViewModel.ConnectAsync` → `AdbCli.ProbeAsync` 找设备 → `adb forward tcp:35353 tcp:22222` → `JCenterDeviceClient.ConnectAsync`。
2. `DashboardViewModel` 启动 `TelemetryPump`；Pump 每 100 ms 调 `HardwareMonitorService.BuildTelemetry()`，`JCenterTelemetryCodec.Build()` 生成 78B 帧，发送到设备。
3. `DisplayViewModel`/`ThemeEditorViewModel` 生成亮度、播放模式、媒体、主题帧；背景媒体先由 `AdbCli.PushAsync` 推到 `/sdcard/`。
4. `SettingsService`/`ThemeStore` 保存 JSON 到 `%LOCALAPPDATA%\JCenterLite\`。
5. `DisplayPage`（默认首页）用 `ThemeCanvas` + `DisplayViewModel.ValueFor` 秒级渲染 480×480 实时画面；主题墙项点击直接调用 `ApplyThemeAsync` 推送到设备。

## 扩展点

- 新命令：在 `JCenterCommands` 加常量，在 `JCenterCommandBuilder`/`JCenterCustomThemeBuilder` 加 builder，并补 `ProtocolTests`。
- 新传感器：在 `HardwareMonitorService.Map` 增加 `(HardwareType, SensorType, Name)` 映射。
- 新 UI：在 `Views/` 加 Page，在 `MainWindow.Nav_SelectionChanged` 注册。
- 主题迁移：`src/JCenterLite.Core/Services/JCenterThemeMigrator.cs`（核心）+ `tools/JCenterThemeMigrator`（CLI）+ 设置页按钮。
