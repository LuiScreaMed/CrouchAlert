# Crouch Alert

English | [简体中文](./README_zh.md)

A Beat Saber mod to alert incoming crouching walls. Supports Beat Saber 1.40.8 on PC.

## Installation

### Dependencies

This mod requires mods below:

- [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloaded)
- [BSML](https://github.com/monkeymanboy/BeatSaberMarkupLanguage)
- [SiraUtil](https://github.com/Auros/SiraUtil)

### Manual Installation

Download the zip file from [Release Page](https://github.com/LuiScreaMed/CrouchAlert/releases), drag in BSManager or extract the zip file in Beat Saber game folder to install.

## Features

- Strict Mode

    When enabled, only consider crouches with full lines of walls.

- Alert Sound

    Play a sound when alert.

Other basic options can be found in in-game settings page (from the left side of the main menu).

The alert sound can be customized by placing `wav` or `ogg` audio files into `UserData/CrouchAlert`.

## Development

Replace `BeatSaberDir` to your beat saber path in [CrouchAlert.csproj.user](./CrouchAlert.csproj.user) before building.

## Credit

The `default.wav` sound was generated in [jsfxr](https://sfxr.me/).
