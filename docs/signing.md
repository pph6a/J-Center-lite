# 代码签名（SignPath Foundation 免费签名）

Windows 上没签名的 exe 会让每个用户看到 SmartScreen 的“未知发布者”警告。
[SignPath Foundation](https://signpath.org/) 为开源项目提供**免费**的代码签名服务：证书由 SignPath Foundation 持有，
他们验证“二进制确实由你的公开仓库构建”，并以此为发布背书。本项目已按它的要求把仓库侧准备好。

## 1. 先决条件（对照 SignPath 的条款）

| SignPath 要求 | 本项目状态 |
|---|---|
| 无恶意代码 | ✅ |
| OSI 认可的开源许可证，且无商业双许可 | ✅ MIT |
| 不含专有组件（System Libraries 除外） | ✅ 自研代码 + MPL-2.0 / MIT / MS 系统库；**不捆绑** adb、驱动、厂商素材 |
| 项目在维护 | ✅ |
| 已经发布过要签名的形态 | ✅ [v1.0.0](https://github.com/pph6a/J-Center-lite/releases/tag/v1.0.0) 便携 zip |
| 有描述功能的下载页 | ✅ README + `docs/device-support.md` |
| 只签自己的项目/二进制，维护者拥有源码与构建脚本 | ✅ 仓库自有 |
| 构建必须跑在 GitHub 托管的 runner 上 | ✅ 工作流全部 `runs-on: windows-latest` |
| **仓库公开** | ⚠️ 当前仍是 private，申请前需转公开 |

> 转公开：`gh repo edit pph6a/J-Center-lite --visibility public --accept-visibility-change-consequences`

## 2. 提交申请

1. 打开 **https://signpath.org/apply**，填写表单（HubSpot 表单，英文）。
2. 建议直接使用下面这段现成文案：

```text
Project name: J-Center-Lite

Repository: https://github.com/pph6a/J-Center-lite

License: MIT

Download page / releases:
https://github.com/pph6a/J-Center-lite/releases

Maintainer (GitHub account): pph6a

Artifacts to be signed:
J-Center-Lite-<version>-win-x64.zip — a portable Windows package that contains
JCenterLite.exe plus its .NET 8 / Windows App SDK runtime files.
Only JCenterLite.exe and JCenterLite.Core.dll (both built from this repository)
need to be signed; upstream and System Library files must stay untouched.

What the project does:
J-Center-Lite is a clean-room WinUI 3 reimplementation of the JONSBO / SEGOTEP
"J-Center" control panel for QiaoSiBo 480x480 secondary display modules
(for example the JONSBO TF2-360SC all-in-one liquid cooler with an IPS screen).
It reads PC sensors through LibreHardwareMonitor and drives the display over an
ADB-forwarded TCP protocol, including theme migration from the vendor tool.
No vendor binaries, drivers, fonts, images, videos or firmware are bundled.

Why we need code signing:
The app is distributed as a portable ZIP, so without a certificate every user
sees a SmartScreen "unknown publisher" warning and cannot verify that the
binary was built from the public repository.

Other components:
- CommunityToolkit.Mvvm, H.NotifyIcon.WinUI — MIT
- LibreHardwareMonitorLib — MPL-2.0
- Microsoft Windows App SDK / .NET runtime — System Libraries
- adb.exe is NOT bundled; users provide their own copy.
```

3. 提交后等待回复（通常几天，可能会追问仓库细节）。

## 3. 审批通过后的配置

### SignPath 侧

1. 安装 **SignPath GitHub App** 并授权访问本仓库（用于验证构建来源）。
2. 在 SignPath 里创建：
   - **Organization** → 记下 Organization ID
   - **Project**（例如 `j-center-lite`）→ 记下 project slug
   - **Signing Policy**（例如 `release-signing`）→ 记下 slug
   - **Artifact Configuration** → 把仓库里 [build/signpath/artifact-configuration.xml](../build/signpath/artifact-configuration.xml)
     的内容粘进去，记下 slug（它只签 `JCenterLite.exe` 与 `JCenterLite.Core.dll`，其余文件一律不动）
3. 创建一个 **API Token**（对该 Project 有 submitter 权限）。

### GitHub 侧

仓库 → Settings → Secrets and variables → Actions：

| 类型 | 名称 | 值 |
|---|---|---|
| Secret | `SIGNPATH_API_TOKEN` | SignPath 里创建的 API Token |
| Variable | `SIGNPATH_ORGANIZATION_ID` | SignPath Organization ID |
| Variable | `SIGNPATH_PROJECT_SLUG` | 例如 `j-center-lite` |
| Variable | `SIGNPATH_SIGNING_POLICY_SLUG` | 例如 `release-signing` |
| Variable | `SIGNPATH_ARTIFACT_CONFIGURATION_SLUG` | 例如 `j-center-lite-zip` |

> 只要 `SIGNPATH_ORGANIZATION_ID` 为空，[sign-release 工作流](../.github/workflows/sign-release.yml) 会直接跳过，不会报错。

## 4. 签名一个版本

Actions → **sign-release** → Run workflow → 填已存在的 Release tag（例如 `v1.0.0`）。

工作流会：

1. 检出该 tag 并用 `tools/build-release.ps1` 重新构建未签名的 zip；
2. 用 `actions/upload-artifact` 上传到 GitHub（SignPath 的连接器要求产物必须先存在于 GitHub）；
3. 调用 `signpath/github-action-submit-signing-request@v3` 请求签名并等待完成；
4. 用签名后的 zip 覆盖 Release 里的同名资产，并更新 `.sha256`。

## 5. 验证签名

```powershell
Get-AuthenticodeSignature .\JCenterLite.exe | Format-List Status, SignerCertificate
# Status           : Valid
# SignerCertificate: CN=SignPath Foundation, ...
```

或者：右键 `JCenterLite.exe` → 属性 → 数字签名，签名者应显示 **SignPath Foundation**。

## 6. 注意事项

- 证书主体是 **SignPath Foundation**（他们等于该版本的发布背书方），所以必须遵守其
  [Code of Conduct / 条款](https://signpath.org/terms)：只签自己的代码、构建必须来自 GitHub 托管的 runner。
- 每次发版都要重新签名（新 tag → 再跑一次 sign-release）。
- 不要签厂商文件；本项目的包内只有 `JCenterLite.exe` 与 `JCenterLite.Core.dll` 属于我们自己。
- 申请被拒时优先检查：仓库是否公开、许可证是否 OSI、是否有未发布的构建产物、README 是否描述了功能。
