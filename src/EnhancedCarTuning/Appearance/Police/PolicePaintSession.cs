using EnhancedCarTuning.Core;
using EnhancedCarTuning.Localization;

namespace EnhancedCarTuning.Appearance.Police
{
    internal sealed class PolicePaintSession
    {
        internal readonly PolicePaintState State;
        internal bool Original { get; private set; }
        internal int OriginalSiren { get; private set; }
        internal bool Confirmed { get; private set; }
        internal bool CanEdit => tuning.CanEdit && State && State.Supported;
        private readonly GarageSession tuning;
        private readonly object purchaseToken = new object();
        private MenuBuy buy;
        private MenuBuy_Item purchase;
        internal PolicePaintSession(PolicePaintState state, GarageSession tuning)
        { State = state; this.tuning = tuning; Original = state.Committed; OriginalSiren = state.CommittedSirenMode; }
        internal void Toggle() { if (CanEdit) State.Preview(!State.Active); }
        internal void RebaseImported()
        { Original = State.Committed; OriginalSiren = State.CommittedSirenMode; Confirmed = false; buy = null; purchase = null; }
        internal void AddPurchaseItems(MenuBuy target)
        {
            buy = target; purchase = null;
            if (!CanEdit || State.Active == Original) return;
            purchase = target.AddIfNotExits(purchaseToken, 0, true, ECTLocalization.T("Полицейская окраска"), null);
        }
        internal void WrapPurchase()
        {
            if (!buy || buy.eventBuy == null) return;
            var callback = buy.eventBuy;
            buy.eventBuy = delegate
            {
                bool accepted = CanEdit && purchase && purchase.isActive();
                bool chosen = accepted ? State.Active : Original;
                int mode = accepted ? State.SirenMode : OriginalSiren;
                State.Preview(chosen, mode); State.SerializePreview = true;
                try { callback(); State.Commit(chosen, mode); Confirmed = true; }
                catch { State.Commit(Original, OriginalSiren); throw; }
                finally { if (State) State.SerializePreview = false; }
            };
        }
        internal void Cancel()
        {
            if (Confirmed || !State) return;
            State.SerializePreview = false; State.Commit(Original, OriginalSiren);
        }
    }
}
