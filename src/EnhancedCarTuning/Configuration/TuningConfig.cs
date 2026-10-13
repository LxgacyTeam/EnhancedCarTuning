using UnityEngine;

namespace EnhancedCarTuning.Configuration
{
    // Fixed mod rules. Only visual preferences are user-adjustable in the BCL Graphics menu.
    internal static class TuningConfig
    {
        internal const bool ChargeForChanges = true;
        internal const int DrivePrice = 3500;
        internal const int WheelAxlePrice = 1700;
        internal const bool AllowDuringEvents = false;
        internal const bool ShowGarageButton = true;

        internal const string VinylResolutionKey = "EnhancedCarTuning.VinylCanvasResolution";
        internal const string PrimitiveTexturesKey = "EnhancedCarTuning.UsePrimitiveTextures";
        internal const int DefaultVinylTextureResolution = 2048;

        internal static int VinylTextureResolution
        {
            get
            {
                int value = PlayerPrefs.GetInt(VinylResolutionKey, DefaultVinylTextureResolution);
                return value == 512 || value == 1024 || value == 2048 || value == 4096
                    ? value : DefaultVinylTextureResolution;
            }
        }
        internal static bool UsePrimitiveTextures => PlayerPrefs.GetInt(PrimitiveTexturesKey, 1) != 0;
    }
}
