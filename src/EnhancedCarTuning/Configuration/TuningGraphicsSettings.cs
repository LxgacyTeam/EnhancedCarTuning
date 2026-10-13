using UnityEngine;

namespace EnhancedCarTuning.Configuration
{
    internal static class TuningGraphicsSettings
    {
        private static bool registered;

        internal static void Register()
        {
            if (registered || NetManager.isServer) return;
            registered = true;

            LegacySettingsMenuHelper.RegisterTextChoice(
                id: "ect.graphics.vinyl_resolution",
                group: "Render",
                position: new Vector2(0f, -656f),
                label: new LegacyLocalizedText("Vinyl canvas resolution", "Разрешение полотна винилов"),
                binding: LegacyTextChoiceSettingBinding.PlayerPrefsIndex(
                    key: TuningConfig.VinylResolutionKey + ".Index",
                    defaultIndex: ResolutionToIndex(TuningConfig.VinylTextureResolution),
                    values: new[]
                    {
                        new LegacyLocalizedText("512", "512"),
                        new LegacyLocalizedText("1024", "1024"),
                        new LegacyLocalizedText("2048", "2048"),
                        new LegacyLocalizedText("4096 (not recommended)", "4096 (не рекомендуется)")
                    },
                    onChanged: index =>
                    {
                        PlayerPrefs.SetInt(TuningConfig.VinylResolutionKey, IndexToResolution(index));
                        PlayerPrefs.Save();
                        EnhancedCarTuning.Appearance.Vinyls.VinylRenderTextures.Clear();
                        foreach (var paint in UnityEngine.Object.FindObjectsOfType<CarPaintItems>())
                            if (paint) paint.UpImage();
                    }));

            LegacySettingsMenuHelper.RegisterToggle(
                id: "ect.graphics.primitive_textures",
                group: "Render",
                position: new Vector2(0f, -697f),
                label: new LegacyLocalizedText("Improved primitive vinyls", "Улучшенные текстуры фигур"),
                binding: LegacyBoolSettingBinding.PlayerPrefs(
                    key: TuningConfig.PrimitiveTexturesKey,
                    defaultValue: true,
                    onChanged: _ => EnhancedCarTuning.Appearance.Vinyls.PrimitiveVinylTextures.RefreshCatalog()));
        }

        private static int ResolutionToIndex(int value) => value == 512 ? 0 : value == 1024 ? 1 : value == 4096 ? 3 : 2;
        private static int IndexToResolution(int index) => index == 0 ? 512 : index == 1 ? 1024 : index == 3 ? 4096 : 2048;
    }
}
