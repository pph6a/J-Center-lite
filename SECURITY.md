# Security Policy

## 报告问题

请通过 GitHub Security Advisories 或私信维护者报告，不要在公开 issue 中粘贴设备序列号、token、cookie 或完整日志。

## 设计边界

- 不捆绑内核驱动；传感器驱动由 LibreHardwareMonitor 按其机制安装。
- 不直接写 EC/MSR 寄存器；不做风扇占空比写入。
- ADB 只调用用户指定的 `adb.exe`，不启动未知二进制。
- 不访问厂商云接口；更新只查 GitHub Releases。

## 用户应注意

- 以管理员运行会扩大传感器驱动的权限面；只在你信任的机器上使用。
- OTA 推送只是把文件放到设备 `/sdcard/ota_package/`，安装流程由设备固件负责。
