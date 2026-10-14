# 合规与安全说明

J-Center-Lite 是独立实现（clean-room-style reimplementation），用于研究并复现 J-Center 面板的**软件行为与互操作协议**。

## 不包含的内容

- 不包含 SEGOTEP / horiemtech 的 `WFanManager.exe`、`DaemonService.exe`、`AutoUpdater.exe`、`*.dll`、`*.sys`；
- 不包含原程序的图片、字体、GIF/MP4、XAML/BAML、APK 或固件；
- 不包含 Google `adb.exe` / platform-tools；
- 不打包 LibreHardwareMonitor 的驱动二进制；由 NuGet 包在运行时按其许可处理。

## 协议互操作

协议结论来自对用户本机已安装程序的反编译与被动抓包，用于让自研客户端与用户自己的设备互通。仓库只描述命令格式与字段，不复制原程序实现代码。

## 风险提示

- 传感器读取可能安装/加载内核驱动；请只在你信任的机器上以管理员运行，并自行评估安全软件拦截。
- 纯 WinRing0 系列驱动社区已知存在安全风险；本项目不直接调用 WinRing0，也不建议重新分发它。
- 不要用本项目写入未知的 OTA/固件包；OTA 推送只是 ADB 文件传输。
- J-Center 是原厂商的产品名；J-Center-Lite 与之无隶属或背书关系。

## 第三方许可

- LibreHardwareMonitorLib：MPL-2.0；
- CommunityToolkit.Mvvm：MIT；
- H.NotifyIcon：MIT；
- Windows App SDK：Microsoft 许可；
- 本项目代码：MIT（见 `LICENSE`）。
