using System;

namespace EnhancedCarTuning.Appearance.Body
{
    internal static class BodyPartNames
    {
        internal static string Label(BodyPart part) => part == BodyPart.Body ? "КУЗОВ" : part == BodyPart.Hood ? "КАПОТ" :
            part == BodyPart.Front ? "ПЕРЕДНИЙ БАМПЕР" : part == BodyPart.Trunk ? "БАГАЖНИК" :
            part == BodyPart.Rear ? "ЗАДНИЙ БАМПЕР" : "ДВЕРИ";
        internal static BodyPart Classify(string name)
        {
            name = name ?? "";
            if (Contains(name, "Bamper_Back")) return BodyPart.Rear;
            if (Contains(name, "Bamper_Front")) return BodyPart.Front;
            if (Contains(name, "Door")) return BodyPart.Doors;
            if (Contains(name, "Hood_Back")) return BodyPart.Trunk;
            if (Contains(name, "Hood_Front")) return BodyPart.Hood;
            return BodyPart.Body;
        }
        private static bool Contains(string name, string detail) => name.IndexOf(detail, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
