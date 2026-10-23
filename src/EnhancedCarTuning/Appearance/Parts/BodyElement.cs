using System;

namespace EnhancedCarTuning.Appearance.Parts
{
    internal enum BodyElement { Hood, Trunk, FrontDoors, RearDoors, FrontBumper, RearBumper, Fenders, Bullbar, Sunroof, Roof, Grille, Cage }
    internal static class BodyElementNames
    {
        internal static readonly BodyElement[] Body = { BodyElement.Hood, BodyElement.Trunk, BodyElement.FrontDoors, BodyElement.RearDoors, BodyElement.FrontBumper, BodyElement.RearBumper, BodyElement.Fenders };
        internal static readonly BodyElement[] Specials = { BodyElement.Bullbar, BodyElement.Sunroof, BodyElement.Roof, BodyElement.Grille, BodyElement.Cage };
        private static bool Has(string text,string token) => text.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0;
        private static bool Token(string text,string token)
        {
            int at=text.IndexOf(token,StringComparison.OrdinalIgnoreCase);
            if(at<0)return false;
            int end=at+token.Length;
            return end==text.Length||text[end]=='_'||text[end]==' '||text[end]=='(';
        }
        internal static int Bed(string name)
        {
            for(int i=1;i<=4;i++)if(Token(name??"","_Kuzov_"+i))return i;
            return 0;
        }
        internal static bool TryClassify(string name,out BodyElement part)
        {
            string n=name??"";part=BodyElement.Hood;
            if(Has(n,"_Hood_Front_UP"))return false;
            if(Has(n,"_Bamper_Back"))part=BodyElement.RearBumper;
            else if(Has(n,"_Bamper_Front"))part=BodyElement.FrontBumper;
            else if(Has(n,"_Door_Left_Back")||Has(n,"_Door_L_Back")||Has(n,"_Door_Right_Back")||Has(n,"_Door_R_Back"))part=BodyElement.RearDoors;
            else if(Has(n,"_Door_Left_Front")||Has(n,"_Door_Right_Front")||Token(n,"_Door_L")||Token(n,"_Door_R")||Token(n,"_Door_Left")||Token(n,"_Door_Right"))part=BodyElement.FrontDoors;
            else if(Has(n,"_Hood_Back"))part=BodyElement.Trunk;
            else if(Has(n,"_Hood_Front"))part=BodyElement.Hood;
            else if(Token(n,"_Wing_L")||Token(n,"_Wing_R")||Token(n,"_Wing_Left")||Token(n,"_Wing_Right"))part=BodyElement.Fenders;
            else if(Has(n,"Karkas")||Has(n,"HAM_Bamper"))part=BodyElement.Cage;
            else if(Has(n,"_Kengurin")||Token(n,"_Bam")||Has(n,"GMC_Bamper"))part=BodyElement.Bullbar;
            else if(Has(n,"_Hood_Top")||Has(n,"_Win_Top"))part=BodyElement.Sunroof;
            else if(Has(n,"_Roof"))part=BodyElement.Roof;
            else if(Has(n,"_Grill"))part=BodyElement.Grille;
            else return false;
            return true;
        }
        internal static string Label(BodyElement part)
        {
            switch(part)
            {
                case BodyElement.Hood:return "КАПОТ";case BodyElement.Trunk:return "БАГАЖНИК";
                case BodyElement.FrontDoors:return "ПЕРЕДНИЕ ДВЕРИ";case BodyElement.RearDoors:return "ЗАДНИЕ ДВЕРИ";
                case BodyElement.FrontBumper:return "ПЕРЕДНИЙ БАМПЕР";case BodyElement.RearBumper:return "ЗАДНИЙ БАМПЕР";
                case BodyElement.Fenders:return "КРЫЛЬЯ";case BodyElement.Bullbar:return "КЕНГУРЯТНИК";
                case BodyElement.Sunroof:return "ЛЮК";case BodyElement.Roof:return "КРЫША";
                case BodyElement.Grille:return "РЕШЕТКА РАД.";default:return "КАРКАС";
            }
        }
    }
}
