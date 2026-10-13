using EnhancedCarTuning.TuneUp.Engine;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Persistence;
using EnhancedCarTuning.Appearance.Police;

namespace EnhancedCarTuning.Networking
{
    internal static class NetworkTuningPolicy
    {
        internal static bool LocalPlayerInEvent()
        {
            var input = InputControl.GetFirstUser();
            return input && input.netInput && input.netInput.curEvent;
        }

        internal static bool Validate(string xml, out TuningSettings settings)
        {
            if (!TuningXmlCodec.TryRead(xml, out settings, out string error))
            { Plugin.Warn("Rejected tuning XML: " + error); return false; }
            if (!PolicePaintXml.TryRead(xml, out _))
            { Plugin.Warn("Rejected invalid police paint XML."); return false; }
            if (!EngineCatalog.IsKnown(settings.EngineId))
            { Plugin.Warn("Rejected unknown engine: " + settings.EngineId); return false; }
            return true;
        }

        internal static bool AcceptChange(NetControl net, string xml)
        {
            if (!Validate(xml, out TuningSettings next)) return false;
            if (EnhancedCarTuning.Configuration.TuningConfig.AllowDuringEvents || !net.netInputControl || !net.netInputControl.curEvent) return true;
            // Let the game's event rules handle paint changes; prohibit only a tuning change here.
            TuningXmlCodec.TryRead(net.xmlData, out TuningSettings previous, out _);
            return next.Equals(previous);
        }
    }
}
