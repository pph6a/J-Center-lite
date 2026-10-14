# 发布到 GitHub

## 仓库现状

| 项目 | 值 |
|---|---|
| 远端 | `git@github.com:pph6a/J-Center-lite.git`（`origin`） |
| 默认分支 | `main` |
| 可见性 | **private**（要公开时再执行本文最后一节） |
| CI | `.github/workflows/ci.yml` — Windows runner：还原 → 构建 → 单测 → 发布并上传 artifact |
| Release | `.github/workflows/release.yml` — 打 `v*` tag 时自动打包并创建 Release |

## 发布一个新版本

1. **改版本号**：`Directory.Build.props` 的 `<Version>`、`CHANGELOG.md`，以及设置页“关于”里的版本文案。
2. **本地验证**：

   ```powershell
   $dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
   & $dotnet build .\src\JCenterLite.App\JCenterLite.App.csproj -c Release -p:Platform=x64
   & $dotnet test  .\tests\JCenterLite.Core.Tests\JCenterLite.Core.Tests.csproj -c Release
   ```

3. **本地打包复核**：

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\tools\build-release.ps1 -Version 1.0.0
   ```

   产物：`dist\J-Center-Lite-1.0.0-win-x64.zip` 与 `dist\J-Center-Lite-1.0.0-win-x64.zip.sha256`。

4. **提交并打 tag**（tag 会触发自动发布）：

   ```powershell
   git add -A
   git commit -m "release: v1.0.0"
   git push
   git tag v1.0.0
   git push origin v1.0.0
   ```

5. **核对发布结果**：

   ```powershell
   gh run list --limit 3
   gh release view v1.0.0
   ```

## Release 说明模板

- 需要自备 `adb.exe`；建议以管理员运行以获得完整传感器。
- 不包含任何厂商二进制、素材、驱动或固件。
- 已知边界见 `docs/feature-matrix.md`；设备支持矩阵见 `docs/device-support.md`。
- 校验下载包：

  ```powershell
  certutil -hashfile J-Center-Lite-1.0.0-win-x64.zip SHA256
  ```

## 发布前检查清单

- [ ] `dotnet build` 与 `dotnet test` 全绿
- [ ] `git status` 干净；`dist/`、`artifacts/`、`spike/`、`bin/`、`obj/` 未入库
- [ ] 仓库里没有 token / cookie / 个人机器路径：

  ```powershell
  git grep -n -E "C:.Users.|gho_|ghp_|password|secret"
  ```

- [ ] README 截图与当前界面一致，且不含厂商素材或用户媒体
- [ ] `CHANGELOG.md` 的版本号与 tag 一致
- [ ] Release 里附了 zip 与 `.sha256`

## 需要公开仓库时

```powershell
gh repo edit pph6a/J-Center-lite --visibility public --accept-visibility-change-consequences
```

公开前请确认：历史提交里没有需要撤回的内容（当前提交只包含自研代码、文档与自绘截图）。
