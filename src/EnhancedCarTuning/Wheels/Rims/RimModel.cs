namespace EnhancedCarTuning.Wheels.Rims
{
    internal sealed class RimModel
    {
        internal readonly string Id, Name, LeftAsset, RightAsset;
        internal readonly float LeftRadius, RightRadius;
        internal RimModel(string id,string name,string left,string right,float leftRadius,float rightRadius)
        { Id=id; Name=name; LeftAsset=left; RightAsset=right; LeftRadius=leftRadius; RightRadius=rightRadius; }
    }
}
