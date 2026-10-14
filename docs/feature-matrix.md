# 功能对照表

状态定义：**已实现** = 可直接使用；**部分** = 有可用实现但有明确缺口；**未实现** = 路线图；**不适用** = 原程序没有该功能。

| 原程序功能 | 证据 | J-Center-Lite | 说明 |
|---|---|---|---|
| CPU/GPU/内存/硬盘/网络传感器 | `GetCPUTemp`、`ShareData` | 已实现 | LibreHardwareMonitor + Windows 原生回退 |
| 风扇/水泵转速读取 | `FanByWinRing`、`GetShareData` | 已实现（取决于硬件） | 通过 LHM SuperIO/主板传感器读取 |
| 传感器 100 ms 推送到副屏 | `App` 定时器、78B 帧 | 已实现 | 帧格式已单测 |
| 亮度控制 | cmd `0x07` | 已实现 | 保存到设置 |
| 开关屏 | cmd `0x24` | 已实现 | |
| 循环/随机/关闭播放 | cmd `0x26/0x27` | 已实现 | |
| 终端版本查询 | cmd `0x2E` | 已实现 | 返回 ASCII |
| 单连接占用检测与接管 | 原程序常驻连接、固件单客户端 | 已实现 | 检测 `WFanManager.exe`；确认后结束它并重连 |
| 环境噪声查询 | cmd `0x2F` | 已实现（Core） | UI 可扩展 |
| 语言切换 | cmd `0x30` | 部分 | 设备语言已实现；应用 UI 目前中文为主 |
| 图片旋转 | cmd `0x32` | 部分 | 编辑器支持元素旋转；图片裁剪 UI 未做 |
| 自定义主题（时间/日期/温度…） | cmd `0x39`、`ThemeElement` | 已实现 | 头帧 + 17 字节元素 + 8 元素分块 |
| 默认主题 1–8 预览 | `UCTheme1..8` | 部分 | 不复制厂商预览图；用户可自建等价主题 |
| 原 J-Center 主题迁移 | `LocalVideo\ThemeImage`、`CustomConfigData.xml`、`Theme\*.xml` | 已实现 | 应用内“设置 → 主题迁移”；媒体副本只写入本机应用数据 |
| GIF/视频背景 | `LocalVideo`、ffmpeg | 已实现 | 直接推送 GIF/MP4/WebM；可选 ffmpeg 路径 |
| 图片裁剪/调整窗口 | `PageTZ` | 未实现 | 路线图 |
| 自动更新 | `AutoUpdater.exe`、`XmlHelper` | 部分 | GitHub Releases 检查；自动下载/替换未做 |
| 开机自启 | `IsOpenStart`、注册表 | 已实现 | HKCU Run |
| 最小化/关闭到托盘 | `NotifyIcon`、`IsMinOrClose` | 已实现 | H.NotifyIcon，托盘菜单可退出 |
| 单实例 | 互斥体（原程序） | 未实现 | 路线图 |
| 副屏重启 | `ADBHelper.ReBootDevice` | 已实现 | `adb shell reboot` |
| OTA 包推送 | `PushOTA` | 部分 | 推送到 `/sdcard/ota_package/`；安装反馈未做 |
| 云主题/服务器下载 | `COSXML` 引用 | 未实现 | 不访问厂商云；路线图 |
| 多语言资源 | `LanguageComboBox` | 部分 | 预留 LanguageIndex；完整 resw 未完成 |
| 风扇占空比控制 | 未找到写入 API | 不适用 | 原程序以监控 + 副屏推送为主 |
| RGB/灯效 | 未找到 | 不适用 | |
