using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.UI
{
    internal static class CurrencyIcons
    {
        internal static Image Find(bool coins)
        {
            string relativePath = "Root/top/root/Shop/" + (coins ? "Coins" : "Almaz") + "/Image (2)";
            var source = FromMenu(MenuEsc.me, relativePath);
            if (source) return source;
            // Also covers inactive menus and the garage opened from the main menu.
            foreach (var menu in UnityEngine.Resources.FindObjectsOfTypeAll<MenuEsc>())
            {
                source = FromMenu(menu, relativePath);
                if (source) return source;
            }
            // Native purchase UI provides the same currency assets if the top bar isn't loaded.
            if (Nuligine.me && Nuligine.me.ui != null && Nuligine.me.ui.menuBuy)
                foreach (var item in Nuligine.me.ui.menuBuy.GetComponentsInChildren<MenuBuy_Item>(true))
                {
                    source = coins ? item.img_coins : item.img_diamonds;
                    if (source && source.overrideSprite) return source;
                }
            return null;
        }

        private static Image FromMenu(MenuEsc menu, string path)
        {
            if (!menu) return null;
            var child = menu.transform.Find(path);
            var image = child ? child.GetComponent<Image>() : null;
            return image && image.overrideSprite ? image : null;
        }
    }
}
