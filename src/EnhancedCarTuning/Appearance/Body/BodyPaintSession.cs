using EnhancedCarTuning.Core;
using EnhancedCarTuning.Localization;

namespace EnhancedCarTuning.Appearance.Body
{
    internal sealed class BodyPaintSession
    {
        internal readonly BodyPaintState State;
        internal BodyPaintSettings Original { get; private set; }
        internal PaintFinish OriginalBase { get; private set; }
        internal PaintFinish Base { get; private set; }
        internal bool Confirmed { get; private set; }
        private readonly GarageSession tuning;
        private readonly object purchaseToken = new object();
        private MenuBuy buy;
        private MenuBuy_Item purchase;
        internal BodyPaintSession(BodyPaintState state, GarageSession tuning)
        { State = state; this.tuning = tuning; Original = state.Committed; state.ApplyBase(Original); Base = OriginalBase = PaintFinish.Capture(state.CarMaterial.body); }
        internal bool CanEdit => tuning.CanEdit;
        internal void RebaseImported()
        {
            Original = State.Committed;
            State.ApplyBase(Original);
            Base = OriginalBase = PaintFinish.Capture(State.CarMaterial.body);
            Confirmed = false; buy = null; purchase = null;
        }
        internal void Paint(int mask, PaintFinish finish)
        {
            if (!CanEdit || mask == 0) return;
            // BaseColor remains an internal compatibility alias of Body, never a separate finish.
            if ((mask & BodyPaintSettings.BaseColor) != 0) mask = (mask & BodyPaintSettings.All) | (1 << (int)BodyPart.Body);
            if ((mask & (1 << (int)BodyPart.Body)) != 0) Base = finish;
            State.Preview(State.Active.WithMask(mask, finish));
            State.CarMaterial.Apply(); State.Invalidate();
        }
        internal PaintFinish Get(BodyPart part) => State.Active[part] ?? Base;
        internal void AddPurchaseItems(MenuBuy target)
        {
            buy = target; purchase = null;
            if (!CanEdit || State.Active.Equals(Original)) return;
            bool charge = EnhancedCarTuning.Configuration.TuningConfig.ChargeForChanges;
            purchase = target.AddIfNotExits(purchaseToken, charge ? 300 : 0, true, ECTLocalization.T("Покраска деталей кузова"), null);
        }
        internal void WrapPurchase()
        {
            if (!buy || buy.eventBuy == null) return;
            var callback = buy.eventBuy;
            buy.eventBuy = delegate
            {
                bool allowed = CanEdit;
                if (!allowed && purchase) { purchase.SetCost(0, true); buy.UpTotalCosts(); }
                var chosen = allowed && purchase && purchase.isActive() ? State.Active : Original;
                Base = chosen[BodyPart.Body] ?? OriginalBase;
                Base.Apply(State.CarMaterial.body);
                State.Preview(chosen); State.SerializePreview = true;
                try { callback(); State.Commit(chosen); Base = PaintFinish.Capture(State.CarMaterial.body); Confirmed = true; }
                catch { Base = OriginalBase; Base.Apply(State.CarMaterial.body); State.Commit(Original); throw; }
                finally { if (State) State.SerializePreview = false; }
            };
        }
        internal void Cancel()
        {
            if (Confirmed || !State) return;
            Base = OriginalBase; Base.Apply(State.CarMaterial.body); State.Preview(Original);
            State.SerializePreview = false; State.CarMaterial.Apply();
        }
    }
}
