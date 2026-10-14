# QiaoSiBo 480×480 display protocol

逆向对象：`C:\Program Files (x86)\horiemtech\J-Center\WFanManager.exe`（J-Center 1.35.55.0，SHA-256 `0680e1f0…0211C270`）。设备：`QiaoSiBo` / `bd09circle_qsb`，Android 8.1，系统应用 `com.hr.cpuinfoshow`。

> 所有结论都来自静态反编译 + 本机被动抓包 + 只读查询。本文只记录协议，不包含原程序代码或资源。

## 1. 连接方式

1. PC 侧 adb 建立转发：`adb forward tcp:3535 tcp:22222`。
2. J-Center 连接 `127.0.0.1:3535`，数据到达设备端 `ServerSocket(22222)`（`MainActivity`）。
3. 媒体文件通过 `adb push` 放到 `/sdcard/<name>`，OTA 放到 `/sdcard/ota_package/`。

实测证据：`adb forward --list` 返回 `HRBDFUN tcp:3535 tcp:22222`；Wireshark 缺失时用“临时接管 3535 端口”的被动抓包取得周期帧；只读注入 `0x2F` 得到 ASCII `13.7`，`0x2E` 返回版本字符串。

## 2. 帧格式

```
+------+------+--------+------+------+------------------+--------+
| 0xAA | 0xF5 | count  | seq  | cmd  | payload ...      | cksum  |
+------+------+--------+------+------+------------------+--------+
  1B     1B     LE16     1B     1B     count*2-5 bytes   1B
```

- `count` 是命令相关的 16 位常量/计数，不是严格的帧总长。短命令为 4；传感器帧为 38；自定义主题头为 42；自定义主题数据块为 `5 + 17×元素数`。
- `seq` 在周期/短命令中为 `0x01`；自定义主题使用 `0x03`。
- `cksum = (sum(帧内除 cksum 外的所有字节)) & 0xFF`。
- 周期传感器帧总长 `78` 字节：`2 (magic) + 2 (count) + 1 (seq) + 1 (cmd) + 71 (payload) + 1 (cksum)`。

## 3. 命令表（已确认）

| cmd | count | 方向 | 用途 |
|---|---|---|---|
| `0x07` | 4 | PC→屏 | 亮度：`00, percent` |
| `0x15` | 38 | PC→屏 | 传感器数据（周期 100 ms） |
| `0x1F` | 35 | PC→屏 | 选择媒体：`slot, name[32]` |
| `0x22` | 41 | PC→屏 | 背景图：`type, showData(u16), unit[3], name[32]` |
| `0x24` | 4 | PC→屏 | 开关屏：`operation` |
| `0x26` | 4 | PC→屏 | 循环播放：`operation` |
| `0x27` | 4 | PC→屏 | 随机播放：`operation` |
| `0x2E` | 4 | 查询 | 终端版本（返回 ASCII） |
| `0x2F` | 4 | 查询 | 环境噪声（返回 ASCII，如 `13.7`） |
| `0x30` | 4 | PC→屏 | 设备语言：`languageIndex` |
| `0x32` | 4 | PC→屏 | 图片旋转角度 |
| `0x35` | 53 | PC→屏 | 主题图片/项目主题 |
| `0x37` | 55 | PC→屏 | 显示时钟开关：`flag, name[32]` |
| `0x38` | 4 | PC→屏 | 设置角度 |
| `0x39` | 42 / `5+17n` | PC→屏 | 自定义主题头 + 数据块 |

## 4. 传感器帧 payload（cmd 0x15，71 字节）

按顺序（小端）：

| 偏移 | 字段 | 单位/说明 |
|---|---|---|
| 0 | surfaceTemperature | 0.1 °C |
| 2 | coreTemperature | 0.1 °C |
| 4 | packageTemperature | 0.1 °C |
| 6 | voltage | 3 字节 LE，mV |
| 9 | cpuPower | 0.1 W |
| 11 | cpuUsage | 0.1 % |
| 13 | memoryUsage | % |
| 15 | cpuSpeed | MHz |
| 17 | reserved u32 | 0 |
| 21/23/25 | reserved u16 ×3 | 0 |
| 27 | ramTotal | GB |
| 29 | gpuTemp | °C |
| 31 | gpuLoad | % |
| 33 | gpuFrequency | MHz |
| 35 | reserved u16 | 0 |
| 37 | cpuFanRpm | RPM |
| 39 | gpuMemoryUsed | MB |
| 41 | gpuMemoryLoad | % |
| 43 | gpuLoadAlt | % |
| 45 | gpuVoltage | mV |
| 47 | gpuPower | 0.1 W |
| 49 | gpuFanRpm | RPM |
| 51 | ramUsed | GB |
| 53 | pumpRpm | RPM |
| 55 | hddTemperature | °C |
| 57 | hddCapacity | GB |
| 59 | hddUsed | GB |
| 61 | hddFree | GB |
| 63 | hddUsage | % |
| 65 | lanUpload | B/s |
| 67 | lanDownload | B/s |
| 69 | ramFree | GB |

实测样本：`surface=430, core=760, package=430, voltage=1272 mV, ramTotal=63, gpuTemp=51, cpuFan=3007, hdd=1907/1792/114 GB`，与 7950X + RTX 4060 Ti + 64 GB 主机一致。

## 5. 自定义主题（cmd 0x39）

先发头帧：`count=42, seq=3, cmd=0x39, payload = 01 01 03, fileName[32], 00 00 00 00`。

再把元素按 **每块 8 个** 分块，每块：

```
count = 5 + 17 × n
seq   = 3
cmd   = 0x39
payload = n, 0, 每个元素 17 字节
```

单个元素 17 字节：`ElementType(1) DataType(1) Unit[3] X(u16) Y(u16) ScaleX×14(u16) Color[6]`。

- `DataType` 与本项目的 `ThemeElementKind` 对应（0=CPU温度、20=时间、21=日期…）。
- `Color` 去掉 `#`，例如 `#FFFFFF → FFFFFF`（6 字节，不足补零）。
- 每块之间原程序等待 100 ms。

## 6. 逆向边界

- `count` 字段的语义仅按观测常量实现；设备固件是否把它当“寄存器字数/类型”尚未证实。
- 设备端 DEX 是 `quicken` 过的 odex，部分方法无法完整反编译；协议由 PC 侧反编译 + 真实抓包交叉验证。
- 未验证：OTA 安装结果、云主题接口、厂商授权逻辑。
