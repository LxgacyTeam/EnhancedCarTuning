using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.Appearance.Vinyls;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Patches
{
    // The game generates the entire catalog in CarGarageUI.InitPaintSprites from CarPaintSprites.folders.
    // Add a native text-vinyl cell immediately after the default bucket (second position). No Canvas asset edits.
    [HarmonyPatch(typeof(CarGarageUI), "InitPaintSprites")]
    internal static class VinylCatalogPatch
    {
        private static void Postfix(CarGarageUI __instance)
        {
            if (!__instance || !__instance.paintSpriteSrc) return;
            try
            {
                var template = __instance.paintSpriteSrc;
                var parent = template.transform.parent as RectTransform;
                if (!parent || parent.Find("ECT.TextVinylCatalogItem")) return;
                var data = new TextVinylData("MAD OUT 2");
                var info = TextVinylTextures.Info(data.Name);
                if (info == null) return;
                var icon = EmbeddedAssets.TextVinylIcon;
                if (!icon) { Plugin.Warn("Missing embedded text vinyl icon; catalog entry skipped."); return; }
                // Native template is inactive after the original InitPaintSprites. Configure the clone first.
                var item = Object.Instantiate(template, parent, false);
                item.name = "ECT.TextVinylCatalogItem";
                item.gameObject.SetActive(false);
                item.spriteInfo = info;
                item.sprite = icon;
                // Do not call SetSprite: it requests an external ResourceLoading path for a generated text texture.
                if (item.rawImage)
                {
                    var resourceLoader = item.rawImage.GetComponent<LoadTextureFromResource>();
                    if (resourceLoader) resourceLoader.enabled = false;
                    item.rawImage.gameObject.SetActive(false);
                }
                if (item.image)
                {
                    item.image.sprite = icon;
                    item.image.preserveAspect = true;
                    item.image.raycastTarget = false;
                    // CarPaintSprite.SetSprite normally calculates the icon's own RectTransform.
                    // We skip SetSprite for generated text textures, so explicitly center this icon.
                    var iconRect = item.image.rectTransform;
                    iconRect.localScale = Vector3.one;
                    iconRect.localRotation = Quaternion.identity;
                    iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = Vector2.zero;
                    item.image.gameObject.SetActive(true);
                }
                if (item.costPanel) item.costPanel.SetActive(false);
                item.isStaickPos = true;
                item.InitParent();
                item.gameObject.SetActive(true);
                // Sibling order is also aligned after positioning the full grid below.

                var tiles = parent.GetComponentsInChildren<CarPaintSprite>(true)
                    .Where(x => x && x != template && x.transform.parent == parent && x.gameObject.activeSelf)
                    .OrderByDescending(x => ((RectTransform)x.transform).anchoredPosition.y)
                    .ThenBy(x => ((RectTransform)x.transform).anchoredPosition.x).ToList();
                tiles.Remove(item);
                tiles.Insert(Mathf.Min(1, tiles.Count), item);
                Vector2 size = ((RectTransform)template.transform).sizeDelta;
                if (size.x <= 0 || size.y <= 0 || parent.sizeDelta.x <= 0) return;
                if (item.image)
                {
                    // Same square fitting as native icons, without inheriting the template's
                    // left/top anchored offset or stretching a 128x128 image.
                    float iconSide = Mathf.Max(1f, Mathf.Min(size.x, size.y) - 22f);
                    item.image.rectTransform.sizeDelta = new Vector2(iconSide, iconSide);
                }
                // Don't reorder native tiles/backgrounds: Canvas draw order and hit-testing
                // can depend on their existing siblings. Only move our new tile beside the bucket.
                if (tiles.Count > 1)
                    item.transform.SetSiblingIndex(tiles[0].transform.GetSiblingIndex() + 1);
                int columns = Mathf.Max(1, (int)(parent.sizeDelta.x / size.x));
                float gap = (parent.sizeDelta.x - size.x * columns) / (columns + 1);
                float xPos = gap, yPos = 0;
                foreach (var tile in tiles)
                {
                    var rect = (RectTransform)tile.transform;
                    rect.sizeDelta = size;
                    rect.anchoredPosition = new Vector2(xPos, yPos);
                    xPos += size.x + gap;
                    if (xPos + size.x >= parent.sizeDelta.x + 5f)
                    { xPos = gap; yPos -= size.y + gap; }
                }
            }
            catch (Exception error) { Plugin.Warn("Unable to inject text vinyl into native catalog: " + error); }
        }
    }
}
