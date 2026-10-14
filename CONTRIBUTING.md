# Contributing

## 基本约定

- 只提交自己创建的内容；不要提交原厂商二进制、反编译产物、抓包、APK、驱动或固件。
- 改动协议时，必须同时更新 `ProtocolTests` 与 `docs/reverse-engineering/protocol.md`。
- UI 改动保持 WinUI 3 原生控件风格；新增用户可见文本优先使用中文，后续统一迁移到 resw。
- 提交信息使用简洁的祈使句，例如 `Add ambient noise query UI`。

## 本地验证

```powershell
dotnet test tests/JCenterLite.Core.Tests/JCenterLite.Core.Tests.csproj -c Release
dotnet build src/JCenterLite.App/JCenterLite.App.csproj -c Release -p:Platform=x64
```
