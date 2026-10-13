### [Русский](./README_RU.md) | [> English <](./README.md) | [简体中文](./README_ZH.md)

---

<div align="center">
  <h1>EnhancedCarTuning</h1>
  <h3>A BepInEx plugin for the Steam version of <b>MadOut2 BigCityOnline</b> v9.4, bringing vehicle tuning features from newer mobile versions of the game.</h3>
  <h3>The plugin requires <a href="https://github.com/LxgacyTeam/BigCityLegacy">BigCityLegacy</a></h3>
</div>

---

## Features

* Engine and drivetrain selection (RWD, FWD, AWD)
* Individual body part painting (bumpers, hood, trunk, doors)
* Flexible suspension settings: height, wheel offset, travel, stiffness, and camber
* Adjustable wheel radius and width
* Separate settings for the front and rear axles
* Text decals with font selection
* Decal symmetry
* Improved primitive textures
* Increased vinyl canvas resolution (configurable)
* Police skins for **Bob**, **Gelik**, and **Baklajan**, with online synchronization of emergency lights
* Full online synchronization of vehicle tuning

---

## Installation and requirements

Requires the original **MadOut2 BigCityOnline v9.4** with [**BigCityLegacy**](https://github.com/LxgacyTeam/BigCityLegacy) version 1.4.0 or later installed.

### Option 1

Use the [BigCityLegacy online installer](https://github.com/LxgacyTeam/BCL-installer/releases/latest/download/BigCityLegacy_Installer.exe): select the "EnhancedCarTuning" checkbox on the additional plugins page and complete the installation.

### Option 2

Download the archive from [Releases](https://github.com/LxgacyTeam/EnhancedCarTuning/releases) and extract it into the game's root directory.

---

## Building

**Requirements:**

- MadOut2 9.4 with BigCityLegacy installed
- .NET SDK

**Build steps:**

Clone and build the project, specifying the **absolute path** to the game's root directory:

```bat
git clone https://github.com/LxgacyTeam/EnhancedCarTuning.git
cd EnhancedCarTuning
dotnet build -c Release -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

> [!NOTE]
> When opening the project in Visual Studio, you **must** set the path to the game's root directory in the `<GameDir>` property in `Directory.Build.props`. All required Unity and BepInEx assemblies will then be referenced automatically.

---

## Terms and conditions

EnhancedCarTuning is distributed as part of the BigCityLegacy project and is subject to all of its terms of use and applicable legal provisions.
See [NOTICE](https://github.com/LxgacyTeam/BigCityLegacy/blob/main/docs/NOTICE.md).
