# 发布到 GitHub

## 1. 本地准备

```powershell
git init
git add .
git commit -m "Initial J-Center-Lite release"
```

仓库已通过 `.gitignore` 排除 `artifacts/`（逆向产物）、`spike/`、`bin/`、`obj/`。不要把原厂商二进制、APK、抓包或反编译代码提交进去。

## 2. 关联远端并推送

```powershell
git remote add origin https://github.com/<your-account>/J-Center-Lite.git
git branch -M main
git push -u origin main
```

> 本机全局策略默认只推内网 Forgejo；如果要把这个项目放 GitHub，请在确认发布内容后再手动添加 GitHub 远端。

## 3. 生成 Release 产物

```powershell
$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
& $dotnet publish .\src\JCenterLite.App\JCenterLite.App.csproj `
  -c Release -r win-x64 -p:Platform=x64 `
  -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None `
  -o .\dist\win-x64
Compress-Archive .\dist\win-x64\* .\dist\J-Center-Lite-win-x64.zip
```

在 GitHub Releases 里创建 tag（如 `v1.0.0`），上传 `J-Center-Lite-win-x64.zip`，并在说明中写明：

- 需要用户自备 `adb.exe` 与管理权限；
- 不含厂商驱动/资源；
- 已知边界见 `docs/feature-matrix.md`。

## 4. 发布前检查

- `dotnet build` 与 `dotnet test` 全绿；
- `git status` 干净，`artifacts/` 与 `dist/` 未入库；
- README 的协议说明与当前实现一致；
- 没有 token、cookie、机器路径或个人信息进入仓库。
