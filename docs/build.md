# 构建与运行

## 环境

- Windows 11 x64（也保留 ARM64 配置）。
- .NET SDK 8.0（实测 8.0.425，位于 `C:\Users\7950X\.dotnet`）。
- Visual Studio 2022 17.12+（Windows App SDK C# 桌面工作负载）。
- Windows App SDK NuGet `1.6.250602001`（项目已固定）。
- 运行传感器需要管理员权限；`adb.exe` 需要用户自备。

## 命令

```powershell
# 还原 + 构建全部
$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
& $dotnet build .\J-Center-Lite.sln -c Release -p:Platform=x64

# 协议单测
& $dotnet test .\tests\JCenterLite.Core.Tests\JCenterLite.Core.Tests.csproj -c Release

# 生成自包含目录（推荐发布形态）
& $dotnet publish .\src\JCenterLite.App\JCenterLite.App.csproj `
  -c Release -r win-x64 -p:Platform=x64 `
  -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None `
  -o .\dist\win-x64
```

产物：`src\JCenterLite.App\bin\x64\Debug\net8.0-windows10.0.19041.0\JCenterLite.exe`（Debug）或 `dist\win-x64\JCenterLite.exe`（Release）。

## 常见问题

- **NuGet 报 H.NotifyIcon 版本不兼容**：本项目固定 `H.NotifyIcon.WinUI 2.3.2`（net8.0-windows10.0.17763）。
- **缺少 WindowsAppSDK 运行时**：项目默认 `WindowsAppSDKSelfContained=true`，发布目录自带运行时。
- **没有温度/风扇**：以管理员运行；LHM 驱动可能被安全软件拦截。
- **找不到设备**：确认 `adb devices -l` 能看到 `QiaoSiBo`；新版本已兼容老 adb 的空格分隔输出。
- **副屏无响应**：副屏固件只接受一个控制连接；若原 J-Center 在运行，使用“连接设备”弹出的接管确认，或先手动退出它。默认本地端口已改为 35353。
