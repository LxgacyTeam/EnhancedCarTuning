using System;
using System.Collections.Generic;
using EnhancedCarTuning.Core;

namespace EnhancedCarTuning.TuneUp.Engine
{
    internal sealed class EngineOption
    {
        internal string Id { get; }
        internal string Label { get; }
        internal EngineProfile Profile { get; }
        internal EngineOption(CarEngine engine)
        {
            Id = engine.name;
            Label = Id.StartsWith("Engine.", StringComparison.Ordinal) ? Id.Substring(7).Replace('_', ' ') : Id;
            Profile = EngineProfile.Capture(engine);
        }
    }

    internal static class EngineCatalog
    {
        // Display names only. Edit the right-hand strings here to rename engines in TUNE-UP.
        // The keys are the original game IDs and must never be renamed: they are used
        // for physics, prices, XML saves and network synchronization.
        private static readonly Dictionary<string, string> displayNames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Engine.Tractor",          "60 HP V6 3.0" },
                { "Engine.70hp.Gaz",         "70 HP L 1.6" },
                { "Engine.120hp",            "120 HP L 2.0" },
                { "Engine.200hp",            "200 HP V6 2.5" },
                { "Engine.300hp",            "300 HP V6 3.5" },
                { "Engine.300hp.Turbo",      "300 HP V6 2.0 Turbo" },
                { "Engine.500hp_MusculCar",  "500 HP V8 3.0" }
            };

        private static readonly List<EngineOption> options = new List<EngineOption>();
        internal static IReadOnlyList<EngineOption> Options { get { EnsureLoaded(); return options; } }

        private static void EnsureLoaded()
        {
            if (options.Count != 0 || !CarData.me) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CarEngine engine in CarEngine.GetEnginesFromCars())
            {
                if (!engine || string.IsNullOrEmpty(engine.name) || !seen.Add(engine.name)) continue;
                options.Add(new EngineOption(engine));
            }
            options.Sort((a, b) => EngineOrder.Compare(a.Id, b.Id));
        }

        internal static bool TryGet(string id, out EngineOption option)
        {
            EnsureLoaded();
            option = options.Find(item => item.Id == id);
            return option != null;
        }

        internal static bool IsKnown(string id) => id == TuningSettings.StockEngine || TryGet(id, out _);
        internal static string Label(string id)
        {
            if (id == TuningSettings.StockEngine) return "Штатный";
            string displayName;
            if (id != null && displayNames.TryGetValue(id, out displayName) &&
                !string.IsNullOrEmpty(displayName))
                return displayName;
            EngineOption option;
            return TryGet(id, out option) ? option.Label : id;
        }
        internal static void Reset() { options.Clear(); }
    }
}
