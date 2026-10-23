using EnhancedCarTuning.UI;
using EnhancedCarTuning.Localization;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal sealed class GlassGaragePrototype : MonoBehaviour
    {
        private CarGarageUI garage;
        private GlassPrototypeState state;
        private Color original;
        private GlassFinish originalFinish;
        private Dictionary<GlassGroup, GlassGroupPaint> originalGroups;
        private bool confirmed;
        private MenuBuy buy;
        private MenuBuy_Item purchase;
        private readonly object token = new object();
        internal GlassPrototypeState State => state;
        internal bool CanEdit => garage && garage.GetComponent<GarageTuningPanel>()?.Session?.CanEdit == true;
        internal void Initialize(CarGarageUI ui)
        {
            Cancel(); garage = ui; state = GlassPrototypeState.Ensure(ui.carPaint);
            original = state.Active; originalFinish = state.ActiveFinish; state.Scan(); confirmed = false; buy = null; purchase = null;
            originalGroups = GlassGroupPaint.Copy(state.ActiveGroups);
        }
        internal void Paint(int mask, Color color, GlassFinish finish, bool changeFinish)
        {
            if (!CanEdit || !state) return;
            for (int i = 0; i < GlassGroupPaint.GroupCount; i++)
            {
                var group = (GlassGroup)i;
                if ((mask & (1 << i)) == 0 || state.GroupCount(group) == 0) continue;
                state.ActiveGroups[group] = new GlassGroupPaint(color, changeFinish ? finish : state.Effective(group).Finish);
            }
        }
        internal void ResetGroups(int mask)
        {
            if (!CanEdit || !state) return;
            for (int i = 0; i < GlassGroupPaint.GroupCount; i++) if ((mask & (1 << i)) != 0 && state.GroupCount((GlassGroup)i) > 0)
                state.ActiveGroups[(GlassGroup)i] = new GlassGroupPaint(new Color(0,0,0,0), null);
        }
        internal void AddPurchaseItems(MenuBuy target)
        {
            buy = target; purchase = null;
            if (!CanEdit || !state || !CarMaterial.isColorChanget(state.Active, original) && GlassFinish.Same(state.ActiveFinish, originalFinish) && GlassGroupPaint.Same(state.ActiveGroups,originalGroups)) return;
            purchase = target.AddIfNotExits(token, 0, true, ECTLocalization.T("Покраска стекол"), null);
        }
        internal void WrapPurchase()
        {
            if (!state || !buy || buy.eventBuy == null) return;
            var callback = buy.eventBuy;
            buy.eventBuy = delegate
            {
                var chosen = CanEdit && purchase && purchase.isActive() ? state.Active : original;
                var finish = CanEdit && purchase && purchase.isActive() ? state.ActiveFinish : originalFinish;
                var groups = GlassGroupPaint.Copy(CanEdit && purchase && purchase.isActive() ? state.ActiveGroups : originalGroups);
                state.ActiveGroups = GlassGroupPaint.Copy(groups);
                state.Active = chosen; state.ActiveFinish = finish; state.SerializePreview = true;
                try { callback(); if (state) { state.Active = state.Committed = chosen; state.ActiveFinish = state.CommittedFinish = finish;
                    state.ActiveGroups = GlassGroupPaint.Copy(groups); state.CommittedGroups = GlassGroupPaint.Copy(groups); } confirmed = true; }
                catch { if (state) { state.Active = original; state.ActiveFinish = originalFinish; state.ActiveGroups = GlassGroupPaint.Copy(originalGroups); } throw; }
                finally { if (state) state.SerializePreview = false; }
            };
        }
        internal void RebaseImported() { if (state) { original = state.Committed; originalFinish = state.CommittedFinish; originalGroups = GlassGroupPaint.Copy(state.CommittedGroups); } confirmed = false; }
        internal void Cancel() { if (!confirmed && state) { state.Active = original; state.ActiveFinish = originalFinish;
            if (originalGroups != null) state.ActiveGroups = GlassGroupPaint.Copy(originalGroups); state.SerializePreview = false; } }
        private void OnDestroy() { Cancel(); }
    }
}
