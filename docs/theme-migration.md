# 原 J-Center 主题迁移

J-Center-Lite 可以把本机原 J-Center 安装目录里的主题迁移到当前用户的应用数据目录。迁移**不修改原安装目录**，也**不会把厂商素材写进项目仓库**。

## 数据来源

| 来源 | 内容 |
|---|---|
| `LocalVideo\ThemeImage\` | 8 个内置主题的 480×480 预览/背景（`logo230313.gif`、`theme2..8`），以及空白主题 |
| `LocalVideo\CustomThemeImage\CustomConfigData.xml` | 自定义媒体主题的标题、设备索引、媒体路径、缩略图、角度、类型 |
| `LocalVideo\CustomThemeImage\*.mp4/gif` | 自定义媒体主题实际使用的视频/GIF |
| `Theme\*.xml` + 同名 `.png` | 自定义布局主题（文本元素、X/Y、缩放、颜色、ZIndex、背景图） |

## 用法

### 应用内

1. 打开 J-Center-Lite → **设置**。
2. 在“主题迁移”卡片点击 **从原 J-Center 导入主题**。
3. 导入完成后切到 **屏幕显示**，主题列表会立即刷新；可以选择主题并“应用主题”。

### 命令行

```powershell
dotnet run --project tools/JCenterThemeMigrator/JCenterThemeMigrator.csproj -c Release -- `
  "C:\Program Files (x86)\horiemtech\J-Center"
```

## 输出与规则

- 主题库：`%LOCALAPPDATA%\JCenterLite\themes.json`
- 媒体副本：`%LOCALAPPDATA%\JCenterLite\media\jcenter-import\`
- 重复执行会先移除 `jc-import-*` 条目，再重新导入；用户自己创建的主题不受影响。
- 缺少的源媒体会记录在导入结果里，不会创建空主题。
- 内置主题以预览图作为背景，并用等价的传感器文本元素重建；它们不是设备端内置渲染器的逐位复刻。
- 静态图片背景使用 `Set_BackgroundImage`，GIF/MP4 使用 `SelectMedia`，与协议保持一致。

## 缩略图

- 迁移会写入 `ThemeDefinition.ThumbnailFile`：图片/GIF 主题用素材自身，布局主题用同名背景图，内置主题用预览图。
- 视频主题（`.mp4`/`.webm`/`.mov`/`.avi`/`.mkv`）在应用内用 `MediaComposition` 抽取首帧，缓存到 `%LOCALAPPDATA%\JCenterLite\media\thumbs\<theme-id>.jpg`；抽取失败时列表显示播放占位图标。
- 480×480 预览会直接播放视频/显示图片背景，静音循环，元素列表与拖拽行为不变。

## 本机实测结果（2026-10-14）

- 导入 23 个 JC 主题：13 个自定义媒体、1 个 `Theme\1.xml` 布局、9 个内置（8 个主题 + 空白）。
- 复制 22 个素材到 `%LOCALAPPDATA%\JCenterLite\media\jcenter-import`。
- 加上应用自带的 2 个默认主题，主题库共 25 个。
- 原配置引用的 `250328175209.gif`、`250531043002.gif` 在源目录中已不存在，未导入。
