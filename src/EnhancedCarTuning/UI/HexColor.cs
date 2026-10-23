using System;
using System.Globalization;
using UnityEngine;
namespace EnhancedCarTuning.UI
{
    internal static class HexColor
    {
        internal static string Format(Color color,bool alpha)=>"#"+(alpha?ColorUtility.ToHtmlStringRGBA(color):ColorUtility.ToHtmlStringRGB(color));
        internal static bool TryParse(string text,float alpha,out Color color)
        {
            color=Color.white;string n=(text??"").Trim();if(n.StartsWith("#",StringComparison.Ordinal))n=n.Substring(1);
            if(n.Length!=6&&n.Length!=8)return false;
            for(int i=0;i<n.Length;i++)if(!((n[i]>='0'&&n[i]<='9')||(n[i]>='a'&&n[i]<='f')||(n[i]>='A'&&n[i]<='F')))return false;
            byte r=byte.Parse(n.Substring(0,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture),g=byte.Parse(n.Substring(2,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture),b=byte.Parse(n.Substring(4,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture);
            float a=n.Length==8?byte.Parse(n.Substring(6,2),NumberStyles.HexNumber,CultureInfo.InvariantCulture)/255f:alpha;
            color=new Color(r/255f,g/255f,b/255f,a);return true;
        }
    }
}
