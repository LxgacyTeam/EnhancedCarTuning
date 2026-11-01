# ECT asset bundles

Use **Unity 2019.1.8f1**, with Windows and Linux Standalone build support installed. This project builds `ect-rims.bundle`, `ect-tires.bundle` and `glass-prototype.bundle` independently for both targets.

From the ECT repository root, run:

```powershell
.\Build-Resources.ps1 -Unity 'C:\Program Files\Unity\Editor\Unity.exe'
```

The script runs Unity without an interactive window and builds both resource assemblies. Unity logs are written to `build-logs`. It does not need the game assemblies to build resources. Build the plugin with `GameDir` afterward.

Manual Unity entry points are `BuildEctAssets.BuildWindows` and `BuildEctAssets.BuildLinux`. They generate meshes, materials and prefabs in `Assets/Generated`, then write bundles to `src/EnhancedCarTuning.Resources/assets/Windows` or `assets/Linux`.

`SourceModels/Rims` contains 41 OBJ pairs. `SourceModels/Tires` contains the common tire pair. The builder reflects OBJ X coordinates and normals into Unity's basis, fixes triangle winding to match the normals, and centers each model on its wheel axis. For both rims and tires, the logical left address loads the source `_R.obj`, and the logical right address loads `_L.obj`. Each rim prefab contains a `Rim` mesh with material slots `Outer`, `Inner`, and a separate `BrakeDisc` object. All 82 prefabs reference one shared pair of brake meshes; each prefab retains its brake position as a local transform. The brake uses `Assets/BrakeDisc_BaseColor-small.png`.

Runtime side selection uses `CarPhWheel.IsLeft` and the initial local `create_rot` captured by the game. `CarControl` has a container root separate from its rotating Body and View; comparing a wheel world axis with that root makes side selection depend on heading. Native left meshes face -X, whereas the retained SB `/left` address faces +X. `WheelSideResolver` maps each asset convention to the physical wheel side for rims and tires without per-vehicle exceptions. Steering, wheel spin and the current rendered pose do not change the selection.

Tire rubber uses the bundled `ECT/TireRubberTexture` shader and `Assets/TireRubber_BaseColor-small.png` albedo atlas. The shader samples `_MainTex` directly, uses diffuse lighting and shadows, and retains its textured pass at shader LOD 100–400. It has no procedural tread or dependency on Standard shader variants. The atlas lower half contains tread; the upper half contains blank rubber without sidewall markings. The builder gives the outer barrel and sidewalls separate UV islands and handles the circumferential seam. Profile adjustment holds the patterned barrel in place and reshapes only the blank sidewalls. The original full-resolution PNG is a source reference and is not included in the tire bundle. [Texture generation prompt and provenance](./TireTexturePrompt.md).

`build-info.json` records the input texture SHA-256 values and the shared brake mesh count. Rebuild both platform resources when either small PNG changes. The runtime tire loader checks the shader and texture names to detect mismatched resource DLLs.

The generated rim catalog and runtime `RimModelIds` must contain the same 41 IDs. `Outer` and `Inner` names are used to select per-material paint property blocks; the brake material is excluded. Prefabs contain no runtime scripts or physics components.

Both platform resource DLLs have the assembly identity `EnhancedCarTuning.Resources`. Install **one** selected variant alongside `EnhancedCarTuning.dll`; do not install both variants in the game's plugin tree. `ResourceContainer.Platform` identifies the selected variant.

## Wheel sizes

The garage shows wheel radius and rim radius in centimetres. Rim XML stores an absolute diameter in the `EnhancedRimSizes` node. Editing the wheel radius in the garage scales the rim diameter by the same ratio, preserving the tire profile. Editing the rim radius separately changes that profile. Loading saved settings applies both stored sizes directly. The rim is constrained to 30–94% of the outer radius.

Stock wheel models retain their own tires, UVs and materials. For a readable native mesh, the metal core is measured from the tire geometry. A radius edit creates an owned mesh copy and continuously remaps its radial coordinates, preserving the outer tire radius, wheel width and connected triangles. Without a radius override, the original mesh remains in use. A native mesh that cannot be measured has no radius control. Imported SB rims use the common deformable tire. Front/rear axle settings, paint, purchase selection, pooling and vehicle XML use the same tuning state. `EnhancedRimPaint` stores independent `Outer` and `Inner` finishes for both axles. Paint changes do not reset suspension physics.

Maximum wheel diameter is 100 cm for Q7, Tahoe, Jeep_Wran, H2, Picap, Zil_130, TruckMedium, Avalanche and Kamaz_5320; 200 cm for Traktor. Their radius limits are 50 and 100 cm respectively. Other vehicles retain the 42 cm radius editor limit. A stock value represented by `null` remains the original wheel's value.
