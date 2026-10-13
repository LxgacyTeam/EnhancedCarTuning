using UnityEngine.EventSystems;

namespace EnhancedCarTuning.Appearance.UI
{
    public sealed class VinylInputModule : BaseInputModule
    {
        public override void Process()
        {
            var selected = eventSystem.currentSelectedGameObject;
            if (selected) ExecuteEvents.Execute(selected, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
        }
    }
}
