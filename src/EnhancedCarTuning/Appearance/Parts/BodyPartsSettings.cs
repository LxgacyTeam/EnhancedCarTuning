using System;
using System.Globalization;
using System.Xml;
using System.IO;

namespace EnhancedCarTuning.Appearance.Parts
{
    internal sealed class BodyPartsSettings : IEquatable<BodyPartsSettings>
    {
        internal const int AllMask=(1<<12)-1;
        internal static readonly BodyPartsSettings Stock=new BodyPartsSettings(0,0,0);
        // Bed 0 means no truck bed; 1–4 select the corresponding ZiL variant.
        internal readonly int Visible, Hidden, Bed;
        internal BodyPartsSettings(int visible,int hidden,int bed)
        { if((visible&~AllMask)!=0||(hidden&~AllMask)!=0||(visible&hidden)!=0||bed<0||bed>4)throw new ArgumentOutOfRangeException();Visible=visible;Hidden=hidden;Bed=bed; }
        internal bool? Get(BodyElement part)
        {int bit=1<<(int)part;return (Visible&bit)!=0?true:(Hidden&bit)!=0?(bool?)false:null;}
        internal BodyPartsSettings With(BodyElement part,bool enabled)
        {int bit=1<<(int)part;return new BodyPartsSettings(enabled?Visible|bit:Visible&~bit,enabled?Hidden&~bit:Hidden|bit,Bed);}
        internal BodyPartsSettings WithBed(int bed)=>new BodyPartsSettings(Visible,Hidden,bed);
        public bool Equals(BodyPartsSettings other)=>other!=null&&Visible==other.Visible&&Hidden==other.Hidden&&Bed==other.Bed;
        public override bool Equals(object other)=>Equals(other as BodyPartsSettings);
        public override int GetHashCode()=>Visible^(Hidden<<12)^Bed;
    }
    internal static class BodyPartsXml
    {
        internal const string Node="EnhancedBodyParts";
        internal static bool TryRead(string text,out BodyPartsSettings settings)
        {
            settings=BodyPartsSettings.Stock;if(string.IsNullOrEmpty(text))return true;
            try
            {
                var doc=new XmlDocument{XmlResolver=null};
                var options=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024};
                using(var input=new StringReader(text))using(var reader=XmlReader.Create(input,options))doc.Load(reader);
                return TryRead(doc.DocumentElement,out settings);
            }
            catch(XmlException){return false;}
        }
        internal static bool TryRead(XmlElement root,out BodyPartsSettings settings)
        {
            settings=BodyPartsSettings.Stock;var nodes=root?.SelectNodes(Node);
            if(nodes==null||nodes.Count==0)return true;
            if(nodes.Count!=1||!(nodes[0] is XmlElement n)||n.GetAttribute("version")!="1"||n.HasChildNodes)return false;
            if(!Number(n,"visible",0,BodyPartsSettings.AllMask,out int visible)||!Number(n,"hidden",0,BodyPartsSettings.AllMask,out int hidden)||
                !Number(n,"bed",0,4,out int bed)||(visible&hidden)!=0)return false;
            settings=new BodyPartsSettings(visible,hidden,bed);return true;
        }
        private static bool Number(XmlElement n,string key,int min,int max,out int value)
        {value=0;string text=n.GetAttribute(key);return text.Length>0&&text.Length<6&&int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value)&&value>=min&&value<=max;}
        internal static void Write(XmlElement root,BodyPartsSettings settings)
        {
            if(root==null)return;
            for(int i=root.ChildNodes.Count-1;i>=0;i--)if(root.ChildNodes[i].Name==Node)root.RemoveChild(root.ChildNodes[i]);
            if(settings.Equals(BodyPartsSettings.Stock))return;
            var n=root.OwnerDocument.CreateElement(Node);n.SetAttribute("version","1");
            n.SetAttribute("visible",settings.Visible.ToString(CultureInfo.InvariantCulture));n.SetAttribute("hidden",settings.Hidden.ToString(CultureInfo.InvariantCulture));
            n.SetAttribute("bed",settings.Bed.ToString(CultureInfo.InvariantCulture));root.AppendChild(n);
        }
    }
}
