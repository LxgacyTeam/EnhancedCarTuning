using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    internal static class PatchInstaller
    {
        internal static void Install(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(CarAwakePatch)).Patch();
            harmony.CreateClassProcessor(typeof(CarPoolPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageTitlePatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageItemsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageBuyPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GaragePointerPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageCameraPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PrimitiveVinylTexturePatch)).Patch();
            harmony.CreateClassProcessor(typeof(PrimitiveCatalogIconPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PrimitiveVinylLoadedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ServerXmlPatch)).Patch();
            harmony.CreateClassProcessor(typeof(LoadedTuningPriorityPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PoliceEquipmentUpgradePatch)).Patch();
            harmony.CreateClassProcessor(typeof(PoliceRemoteHornPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PoliceRemoteSirenStatePatch)).Patch();
            harmony.CreateClassProcessor(typeof(DeliveryPatch)).Patch();
            harmony.CreateClassProcessor(typeof(NetworkStopPatch)).Patch();
            harmony.CreateClassProcessor(typeof(WheelCamberPatch)).Patch();
            harmony.CreateClassProcessor(typeof(MenuPreviewPositionPatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylSavePatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylChangesPatch)).Patch();
            harmony.CreateClassProcessor(typeof(TextVinylLookupPatch)).Patch();
            harmony.CreateClassProcessor(typeof(TextVinylTexturePatch)).Patch();
            harmony.CreateClassProcessor(typeof(TextVinylLoadedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylSymmetryPatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylSelectionPatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylClonePatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylTypingPatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylCatalogPatch)).Patch();
            harmony.CreateClassProcessor(typeof(GarageMenuTogglePatch)).Patch();
            harmony.CreateClassProcessor(typeof(VinylTextureQualityPatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintSavePatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintPurchasePatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintMaterialPatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintBindingsPatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintMeshPatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintVinylPatch)).Patch();
            harmony.CreateClassProcessor(typeof(BodyPaintPickerPatch)).Patch();
        }
    }
}
