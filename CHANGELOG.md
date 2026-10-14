# Changelog

## 1.0.0 — 2026-10-14

### Added

- WinUI 3 shell（NavigationView + Mica + 自定义标题栏）。
- 硬件监控页：CPU/GPU/内存/系统盘/网络/风扇传感器，LibreHardwareMonitor + Windows 回退。
- 副屏协议：78 字节传感器帧、亮度、播放模式、开关屏、语言、版本查询、自定义主题。
- 可视化 480×480 主题编辑器：元素选择/添加/删除、拖拽、字号、缩放、旋转、颜色、背景导入。
- 媒体推送：PNG/JPG/GIF/MP4/WebM → `/sdcard/`，支持 OTA 包推送。
- 设置持久化、开机自启、托盘最小化/关闭、GitHub 更新检查。
- `JCenterLite.Core` 协议/服务层与 xUnit 单测。
- 逆向协议文档、功能对照表、合规说明、GitHub Actions CI。
- 原 J-Center 主题迁移器：内置主题、自定义媒体主题、`Theme\*.xml` 布局主题；设置页可一键导入并立即刷新主题列表。
- 主题缩略图：迁移时写入 `ThumbnailFile`，视频主题在应用内抽取首帧并缓存到 `%LOCALAPPDATA%\JCenterLite\media\thumbs`。
- 副屏显示页（默认首页）：480×480 实时预览（背景媒体 + 真实传感器数值）+ 缩略图主题墙，单击缩略图即切换副屏主题。
- `tools/install-start-menu-shortcut.ps1`：注册开始菜单快捷方式，可顺带请求固定到“开始”屏幕。
- 主题编辑器重做：元素列表显示中文类型名、颜色条与文本预览；属性面板实时联动（类型可改、可直接填 X/Y、上移/下移/复制元素）；主题可复制、删除带确认、背景可清除。
- `docs/device-support.md`：JONSBO TF2-360SC 实测支持矩阵、固件限制说明与测试机配置。
- README 新增作者自述与界面截图（`docs/images/`：副屏显示、硬件监控、主题编辑器、设置），截图使用演示主题库、不含厂商素材。
- 发布链路：`tools/build-release.ps1`（自包含 win-x64 打包 + SHA256）、`.github/workflows/release.yml`（打 `v*` tag 自动创建 Release）、工程统一版本号 `1.0.0`。
- README 新增“下载与安装”；设备文档补充京东型号链接。
- 未处理异常（UI 线程、后台线程、未观察任务）写入 `%LOCALAPPDATA%\JCenterLite\logs\crash.log`；UI 线程异常不再直接关闭窗口。

### Known limitations

- 单实例、图片裁剪 UI、完整中英双语资源、云主题、自动下载安装更新仍在路线图中。

### Fixed

- 兼容旧版 adb 的 `devices -l` 空格分隔输出（此前会被误判为无设备）。
- 默认本地转发端口改为 35353，自动避让被占用的端口。
- 新增原 J-Center 单连接占用检测与确认接管流程。
- 主题列表与 480×480 预览一片空白：预览画布现在渲染图片/GIF/视频背景（视频循环、静音），列表逐行显示缩略图。
- 启动窗口尺寸过小且标题栏被遮挡：改为按显示器 DPI 计算 1440×900 逻辑像素、在工作区内居中，并为系统最小化/最大化/关闭按钮预留右侧内边距。
- 时钟/日期元素不再把占位文本与实时值拼接（曾显示为 `12:00 17:52`）；旧主题库加载时会自动清掉不适用的元素单位。
- 并发读取传感器加锁：遥测推送、硬件监控页与画面预览同时读取 LibreHardwareMonitor 不再互相干扰。
- 主题元素/主题模型改为可通知（INotifyPropertyChanged），修正“选了另一个元素属性面板不刷新”和“改完属性要手点刷新预览”的问题；画布改为增量重绘，不重启视频。
- 修正给 `StackPanel` 设置 `IsEnabled` 导致 XAML 编译失败的问题（WinUI 3 中 `IsEnabled` 只存在于 `Control`，现改用 `ContentControl` 包裹）。
- 修正主题编辑器“添加/删除/复制元素后左侧元素列表不更新”的问题（`ThemeDefinition.Elements` 改为 `ObservableCollection`），新元素会自动滚动到可见处。
- 修复切换主题时的闪退：预览不再每秒重建视频管线（低频传感器刷新只重绘文本），并把 `MediaPlayer` 的析构改成先脱离可视树再释放。
- 修复高缩放（150%/200%）下页面被压扁：导航栏收窄到 248，设备控制区压缩为单卡片三行，主题墙改为 4 列紧凑缩略图，不再需要滚动才能看到按钮。
