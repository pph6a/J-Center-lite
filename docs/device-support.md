# 设备支持说明

## 作者自述

> 我是自己嫌那个驱动不太好用，重构了一下。

> *"I rebuilt it myself because I didn't like how the vendor driver worked."*

原厂 J-Center 能干活，但界面、交互和多屏/多主题管理都不太顺手，所以我按它的协议重新实现了一个 WinUI 3 版本：
不捆绑厂商二进制、不装额外驱动，用 ADB 直接接管那块 480×480 的屏幕。下面是我这台机器的完整实测情况。

## 我的设备

| 项目 | 内容 |
|---|---|
| 商品 | **乔思伯（JONSBO）TF2-360SC(WI) 白色 一体式水冷散热器** |
| 商品页关键规格 | 魔法拼接风扇 / 铝合金冷头 / **IPS 液晶屏** / 软管 |
| 屏幕 | 480 × 480 IPS，屏内是 Android，可通过 ADB 访问 |
| ADB 设备标识 | `HRBDFUN` — `product:full_bd09circle_qsb`，`model:QiaoSiBo`，`device:bd09circle_qsb` |
| 控制通道 | `adb forward tcp:<本地端口> tcp:22222`，屏侧私有 TCP 协议（帧格式见 [reverse-engineering/protocol.md](reverse-engineering/protocol.md)） |
| 固件特征 | 只接受**一个**控制连接；屏内 Android 的 `dumpsys display` 显示该屏 `state OFF`，画面由固件侧渲染 |

> 因为画面不走 Android framebuffer，`adb exec-out screencap -p` 只能拿到 480×480 全黑图。
> 所以 J-Center-Lite 的"副屏显示"页面用的是**本机复刻渲染**（背景媒体 + 实时传感器数值），而不是抓屏回读。

## 我的测试机（本机实测环境）

| 部件 | 配置 |
|---|---|
| CPU | AMD Ryzen 9 7950X（16 核 32 线程） |
| 主板 | ASUS ROG STRIX X670E-A GAMING WIFI |
| 显卡 | NVIDIA GeForce RTX 4060 Ti（驱动 32.0.16.1047）+ AMD Radeon 核显 |
| 内存 | 32 GB × 2 = 64 GB，Asgard VAM5UH64C32BG-CVARWA @ 5600 MT/s |
| 系统盘 | 致态 TiPlus7100 2 TB |
| 操作系统 | Windows 11 专业版（build 26300） |
| 显示缩放 | 200%（4K），界面按 DPI 自适应 |
| 备注 | 机器上另有若干虚拟显示驱动（Oray / SudoMaker / GameViewer / Honor），与本工具无关 |

- 传感器读取（CPU 温度、功耗、风扇等）在**管理员权限**下最完整；非管理员时内存/磁盘/网络与部分 GPU 传感器仍可用。
- 实测传感器来源：LibreHardwareMonitor（CPU 由 AMD 处理器传感器提供，GPU 由 NVIDIA 驱动提供）。

## 支持情况

| 功能 | 状态 | 说明 |
|---|---|---|
| 设备连接与接管 | ✅ 实测通过 | `adb forward` 自动选空闲端口（默认 35353）；原 J-Center 占用时提示确认后接管 |
| 传感器推送 | ✅ 实测通过 | 默认 100 ms 一帧、78 字节，含 CPU/GPU/内存/硬盘/网络/风扇共 34 个字段 |
| 亮度调节 | ✅ 实测通过 | 协议指令 |
| 开 / 关屏幕 | ✅ 实测通过 | 协议指令 |
| 重启副屏 | ✅ 实测通过 | `adb shell reboot` |
| 查询终端版本 | ✅ 实测通过 | 屏侧回读 |
| 播放模式（循环 / 随机 / 关闭） | ✅ 实测通过 | 影响屏内媒体播放 |
| 内置主题迁移 | ✅ 实测通过 | 从原安装目录导入 8 个内置主题 + 空白主题（作为模板重建） |
| 自定义媒体主题迁移 | ✅ 实测通过 | 导入 13 个自定义主题（MP4/GIF）+ 素材副本 |
| `Theme\*.xml` 布局主题迁移 | ✅ 实测通过 | 导入 1 个布局主题（元素位置/颜色/字号/ZIndex） |
| 主题缩略图 | ✅ 实测通过 | 图片/GIF 用素材本身；视频用 `MediaComposition` 抽首帧并缓存 |
| 自定义媒体推送 | ✅ 实测通过 | PNG/JPG/GIF/MP4/WebM → `adb push` 到 `/sdcard/` 后 `SelectMedia` |
| 实时预览（480×480） | ✅ 本机渲染 | 背景媒体 + 真实传感器数值，秒级刷新 |
| OTA 包推送 | ⚠️ 仅推送 | 可把 `.zip/.bin/.img/.apk` 推到 `/sdcard/ota_package/`；未验证屏侧升级流程 |
| 界面语言命令 | ⚠️ 已实现未实测效果 | 命令可发，屏侧视觉效果未逐一核对 |
| 屏幕画面回读（截屏） | ❌ 不支持 | 固件不暴露 framebuffer（见上文），用本机实时预览替代 |
| 风扇 / 水泵 PWM 调速 | ❌ 不做 | 原厂驱动同样没有该界面；本项目不写 EC 寄存器 |
| 其他 480×480 QiaoSiBo 模组 | ⚠️ 未验证 | 协议字段看起来通用，但只在 TF2-360SC 上实测过 |

## 复现步骤

```powershell
# 1. 确认屏被系统识别
adb devices -l
#    应看到 HRBDFUN ... model:QiaoSiBo

# 2. 启动应用（建议管理员），在"设置"里指定 adb.exe 路径
#    默认本地转发端口 35353 → 屏侧 22222

# 3. 需要的话从原 J-Center 目录迁移主题
dotnet run --project tools/JCenterThemeMigrator/JCenterThemeMigrator.csproj -c Release -- `
  "C:\Program Files (x86)\horiemtech\J-Center"
```

## 反馈

如果你的水冷模组不是 TF2-360SC，但同样是 480×480 的 QiaoSiBo 屏幕（`model:QiaoSiBo`），欢迎开 issue 附上：

- `adb devices -l` 的完整输出
- 设备信息页截图（水冷型号 / 屏幕尺寸）
- 哪些功能可用、哪些不可用

这样可以把支持矩阵补全到更多型号。
