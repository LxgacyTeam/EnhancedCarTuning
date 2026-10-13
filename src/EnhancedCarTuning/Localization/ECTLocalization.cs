using System;
using System.Collections.Generic;

namespace EnhancedCarTuning.Localization
{
    internal static class ECTLocalization
    {
        // The game supplies its language through SettingsValues. Use English for untranslated locales.
        private static readonly Dictionary<string, string> en = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Полицейская окраска", "Police paint" },
            { "ПОКРАСКА", "PAINT" }, { "ПОКРАСКА АВТО", "CAR PAINT" },
            { "ПОКРАСКА КУЗОВА", "BODY PAINT" }, { "ОБЩИЙ ЦВЕТ", "BASE COLOR" },
            { "КУЗОВ", "BODY" }, { "КАПОТ", "HOOD" }, { "ПЕРЕДНИЙ БАМПЕР", "FRONT BUMPER" },
            { "БАГАЖНИК", "TRUNK" }, { "ЗАДНИЙ БАМПЕР", "REAR BUMPER" }, { "ДВЕРИ", "DOORS" },
            { "ВЫБРАТЬ НЕСКОЛЬКО", "SELECT MANY" }, { "Покраска деталей кузова", "Paint body parts" },
            { "ЦВЕТ", "COLOR" }, { "ВИНИЛЫ", "VINYLS" }, { "ТЕКСТ", "TEXT" },
            { "СИММЕТРИЯ", "SYMMETRY" }, { "СИММЕТРИЯ ✓", "SYMMETRY ✓" },
            { "Введите текст", "Enter text" }, { "ПОДВЕСКА", "SUSPENSION" },
            { "ОБЕ ОСИ", "BOTH AXLES" }, { "ПЕРЕДНЯЯ ОСЬ", "FRONT AXLE" },
            { "ЗАДНЯЯ ОСЬ", "REAR AXLE" }, { "СБРОС ОСИ", "RESET AXLE" },
            { "РАДИУС", "RADIUS" }, { "ШИРИНА", "WIDTH" },
            { "СМЕЩЕНИЕ X", "OFFSET X" }, { "СМЕЩЕНИЕ Y", "OFFSET Y" },
            { "ХОД ПОДВЕСКИ", "SUSPENSION TRAVEL" }, { "ЖЁСТКОСТЬ", "STIFFNESS" },
            { "РАЗВАЛ", "CAMBER" }, { "ДВИГАТЕЛЬ", "ENGINE" }, { "ПРИВОД", "DRIVE" },
            { "Разные", "Mixed" }, { " см", " cm" }
        };
        internal static bool IsRussian
        {
            get
            {
                try
                {
                    var settings = SettingsValues.me;
                    if (settings == null || settings.user == null) return false;
                    string language = settings.user.language.GetValue();
                    return !string.IsNullOrEmpty(language) &&
                        (language.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ||
                         language.Equals("Russian", StringComparison.OrdinalIgnoreCase) ||
                         language.Equals("Русский", StringComparison.OrdinalIgnoreCase));
                }
                catch { return false; }
            }
        }
        internal static string T(string text)
        {
            if (string.IsNullOrEmpty(text) || IsRussian) return text;
            string result;
            if (en.TryGetValue(text, out result)) return result;
            int newline = text.IndexOf('\n');
            if (newline > 0) return T(text.Substring(0, newline)) + text.Substring(newline);
            // Units are supplied by the native wheel-slider code.
            return text;
        }
    }
}
