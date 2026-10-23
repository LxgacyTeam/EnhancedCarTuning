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
* Removable body parts
* Flexible suspension settings: height, wheel offset, travel, stiffness, and camber
* Adjustable wheel radius and width
* 41 rim models with separate outer and inner finishes
* Tire profile adjustment for stock wheels and new rims
* Window and front/rear light tint
* Decals on windows
* Separate settings for the front and rear axles
* Text decals with font selection
* Color picker HEX display, copying and entry
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

Use `-p:ResourcesPlatform=Windows` or `-p:ResourcesPlatform=Linux`; the build host's OS supplies the default. Output is in `src/EnhancedCarTuning/bin/Release/Windows` or `Linux`, containing the plugin and `EnhancedCarTuning.Resources.dll`. Install the resource variant matching the game's platform.

Resource bundles are built separately for each platform. Rebuilding them requires Unity **2019.1.8f1** with Windows and Linux Standalone support. Run `Build-Resources.ps1` from the repository root; pass the editor path with `-Unity` when needed. See the [asset build guide](../GlassShaderProject/README.md).

The garage displays radii in centimetres. Maximum wheel diameter is 100 cm for Q7, Tahoe, Jeep_Wran, H2, Picap, Zil_130, TruckMedium, Avalanche and Kamaz_5320, and 200 cm for Traktor. Stock wheel models retain their own tires; there is no tire selector. Canvas resolution applies to every decal type. New text layers start at scale `0.2`. Decal opacity remains `0–1` in the UI, with a `0.5` rendering multiplier on windows.

> [!NOTE]
> When opening the project in Visual Studio, you **must** set the path to the game's root directory in the `<GameDir>` property in `Directory.Build.props`. All required Unity and BepInEx assemblies will then be referenced automatically.

---

## Terms and conditions

EnhancedCarTuning is distributed as part of the BigCityLegacy project and is subject to all of its terms of use and applicable legal provisions.
See [NOTICE](https://github.com/LxgacyTeam/BigCityLegacy/blob/main/docs/NOTICE.md).
