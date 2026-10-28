# Crouch Alert

English | [简体中文](./README_zh.md)

**INCOMING CROUCH!!!**

A Beat Saber mod to alert incoming crouching walls. Supports Beat Saber 1.40.8 on PC. May not work correctly in NoodleExtensions maps.

## Installation

### Dependencies

This mod requires mods below:

- [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded)
- [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage)
- [SiraUtil](https://github.com/Auros/SiraUtil)
- [CustomJSONData](https://github.com/Aeroluna/CustomJSONData)

### Manual Installation

Download the zip file from [Release Page](https://github.com/LuiScreaMed/CrouchAlert/releases), drag in BSManager or extract the zip file in Beat Saber game folder to install.

## Features

- **Alert Duration (In beats/In seconds)**

    Alert crouches ahead configured beats / seconds.

- **Strict Mode**

    When enabled, only consider crouches with full lines of walls.

- **Alert Sound**

    Play a configured sound when alert.

Other basic options can be found in in-game settings page (from the left side of the main menu).

The alert sound can be customized by placing `wav` or `ogg` audio files into `UserData/CrouchAlert`.

## Development

Before building, create the file `CrouchAlert.csproj.user` in the project directory and add your beat saber path into it:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <PropertyGroup>
        <!-- Change this to the path to your beat saber. -->
        <BeatSaberDir>Path/To/Your/BeatSaber</BeatSaberDir>
    </PropertyGroup>
</Project>
```

## Credit

The `default.wav` sound was generated in [jsfxr](https://sfxr.me/).
