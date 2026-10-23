### [Русский](./README_RU.md) | [English](./README.md) | [> 简体中文 <](./README_ZH.md)

---

<div align="center">
  <h1>EnhancedCarTuning</h1>
  <h3>适用于 Steam 版 <b>MadOut2 BigCityOnline</b> v9.4 的 BepInEx 插件，将较新移动版游戏中的车辆改装功能移植到此版本。</h3>
  <h3>本插件需要安装 <a href="https://github.com/LxgacyTeam/BigCityLegacy">BigCityLegacy</a></h3>
</div>

---

## 功能

* 选择发动机和驱动方式（RWD 后轮驱动、FWD 前轮驱动、AWD 全轮驱动）
* 车身部件独立喷漆（保险杠、引擎盖、后备箱盖、车门）
* 可拆卸的身体部位
* 灵活调整悬架：高度、车轮偏距、行程、刚度和外倾角
* 调整车轮半径和宽度
* 41款轮毂样式，支持内外侧独立涂装
* 可调整原厂轮毂及新轮毂的轮胎轮廓
* 车窗及前后灯具熏黑/贴膜
* 车窗贴花
* 分别调整前轴和后轴
* 支持选择字体的文字贴花
* 颜色选择器：HEX 值显示、复制与输入
* 贴花对称功能
* 优化的几何图形纹理
* 更高的贴花画布分辨率（可调）
* 为 **Bob**、**Gelik** 和 **Baklajan** 启用警车涂装，并在联机模式下同步警灯状态
* 在联机模式下完整同步车辆改装数据

---

## 安装与要求

需要原版 **MadOut2 BigCityOnline v9.4**，并安装 1.4.0 或更高版本的 [**BigCityLegacy**](https://github.com/LxgacyTeam/BigCityLegacy)。

### 方式一

使用 [BigCityLegacy 在线安装程序](https://github.com/LxgacyTeam/BCL-installer/releases/latest/download/BigCityLegacy_Installer.exe)：在选择附加插件的页面勾选 "EnhancedCarTuning"，然后完成安装。

### 方式二

从 [Releases](https://github.com/LxgacyTeam/EnhancedCarTuning/releases) 下载压缩包，并解压到游戏根目录。

---

## 构建

**要求：**

- 已安装 BigCityLegacy 的 MadOut2 9.4
- .NET SDK

**构建步骤：**

克隆并构建项目，同时指定游戏根目录的**绝对路径**：

```bat
git clone https://github.com/LxgacyTeam/EnhancedCarTuning.git
cd EnhancedCarTuning
dotnet build -c Release -p:GameDir="path\to\game\dir" -p:CopyToPlugins=true
```

使用 `-p:ResourcesPlatform=Windows` 或 `-p:ResourcesPlatform=Linux` 选择目标平台；默认与构建主机的操作系统一致。输出位于 `src/EnhancedCarTuning/bin/Release/Windows` 或 `Linux`，包含插件及 `EnhancedCarTuning.Resources.dll`。游戏中只安装与运行平台对应的资源版本。

每个平台使用独立的 AssetBundle。重新构建资源需要 Unity **2019.1.8f1**，并安装 Windows 与 Linux Standalone 支持模块。在项目根目录运行 `Build-Resources.ps1`，可通过 `-Unity` 指定编辑器路径。详见[资源构建说明](../GlassShaderProject/README.md)。

车库尺寸以半径显示，单位为厘米。Q7、Tahoe、Jeep_Wran、H2、Picap、Zil_130、TruckMedium、Avalanche、Kamaz_5320 的最大车轮直径为 100 厘米，Traktor 为 200 厘米。原版车轮保留各自的轮胎，不提供独立轮胎选择。画布分辨率适用于所有贴花类型。新文字层默认缩放为 `0.2`。贴花不透明度滑块仍为 `0–1`，在车窗上渲染时乘以 `0.5`。

> [!NOTE]
> 在 Visual Studio 中打开项目时，**必须**在 `Directory.Build.props` 文件的 `<GameDir>` 属性中指定游戏根目录的路径。随后会自动引用所有必需的 Unity 和 BepInEx 程序集。

---

## 条款与条件

EnhancedCarTuning 作为 BigCityLegacy 项目的一部分发布，适用该项目的全部使用条款及相关法律规定。
参见 [NOTICE](https://github.com/LxgacyTeam/BigCityLegacy/blob/main/docs/NOTICE_ZH.md)。
