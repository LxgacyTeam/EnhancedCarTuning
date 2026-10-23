using EnhancedCarTuning.Core;
using EnhancedCarTuning.Localization;
namespace EnhancedCarTuning.Appearance.Parts
{
    internal sealed class BodyPartsSession
    {
        internal readonly BodyPartsState State;
        internal BodyPartsSettings Original {get;private set;}
        internal bool CanEdit=>tuning.CanEdit&&State;
        private readonly GarageSession tuning;
        private readonly object token=new object();
        private MenuBuy buy;private MenuBuy_Item item;private bool confirmed;
        internal BodyPartsSession(BodyPartsState state,GarageSession tuning){State=state;this.tuning=tuning;Original=state.Committed;}
        internal void RebaseImported(){Original=State.Committed;confirmed=false;buy=null;item=null;}
        internal void Toggle(BodyElement part){if(CanEdit&&(State.Available&(1<<(int)part))!=0)State.Preview(State.Active.With(part,!State.IsVisible(part)));}
        internal void StepBed(int direction)
        {
            if(!CanEdit)return;var options=State.Beds;if(options.Length<2)return;
            int index=System.Array.IndexOf(options,State.CurrentBed);index=(index+direction+options.Length)%options.Length;
            State.Preview(State.Active.WithBed(options[index]));
        }
        internal void AddPurchaseItems(MenuBuy target)
        {buy=target;item=null;if(CanEdit&&!State.Active.Equals(Original))item=target.AddIfNotExits(token,0,true,ECTLocalization.T("Детали кузова"),null);}
        internal void WrapPurchase()
        {
            if(!buy||buy.eventBuy==null)return;var callback=buy.eventBuy;
            buy.eventBuy=delegate
            {
                var chosen=CanEdit&&item&&item.isActive()?State.Active:Original;
                State.Preview(chosen);State.SerializePreview=true;
                try{callback();State.Commit(chosen);confirmed=true;}
                catch{State.Commit(Original);throw;}
                finally{if(State)State.SerializePreview=false;}
            };
        }
        internal void Cancel(){if(confirmed||!State)return;State.SerializePreview=false;State.Commit(Original);}
    }
}
