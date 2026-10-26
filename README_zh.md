# 下蹲预警

[English](./README.md) | 简体中文

一个用于警告即将到来的需要下蹲的墙的节奏光剑 Mod。支持 PC 版本节奏光剑 1.40.8。在 Noodle/Mapping Extensions 谱面中可能无法正常工作。

## 安装

### 依赖

该 Mod 依赖以下 Mod：

- [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded)
- [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage)
- [SiraUtil](https://github.com/Auros/SiraUtil)
- [CustomJSONData](https://github.com/Aeroluna/CustomJSONData)

### 手动安装

从 [发布页](https://github.com/LuiScreaMed/CrouchAlert/releases) 下载压缩包，拖入 BSManager 或解压到节奏光剑游戏根目录进行安装。

## 功能

- **预警时长（按照节拍/秒）**

    在配置的节拍/秒之前对下蹲进行预警

- **严格模式**

    启用后，仅在一整排墙的情况下判断下蹲

- **预警音效**

    在预警时播放配置好的音效

其他基本选项可在游戏内设置菜单找到（主菜单左侧）。

预警音效可以通过将 `wav` 或 `ogg` 音频文件放入 `UserData/CrouchAlert` 进行自定义

## 开发

在构建前，在项目文件夹创建 `CrouchAlert.csproj.user`，并添加你的节奏光剑路径：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <PropertyGroup>
        <!-- Change this path if necessary. -->
        <BeatSaberDir>Path/To/Your/BeatSaber</BeatSaberDir>
    </PropertyGroup>
</Project>
```

## 鸣谢

`default.wav` 音效通过网站 [jsfxr](https://sfxr.me/) 生成。
