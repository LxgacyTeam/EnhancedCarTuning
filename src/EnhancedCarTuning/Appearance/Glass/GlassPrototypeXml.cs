using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using EnhancedCarTuning.Appearance.Vinyls;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal static class GlassPrototypeXml
    {
        internal const string Node = "EnhancedGlassPaint";
        internal static void Write(CarPaintItems paint, XmlElement root)
        {
            if (root == null) return;
            var state = GlassPrototypeState.Ensure(paint);
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--) if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            var items = root.SelectSingleNode("CarPaint/Items") as XmlElement;
            if (items == null || items.ChildNodes.Count != paint.items.Count) throw new InvalidOperationException("Glass prototype cannot identify native XML items.");
            var tint = state.SerializePreview ? state.Active : state.Committed;
            var finish = state.SerializePreview ? state.ActiveFinish : state.CommittedFinish;
            var groups = state.SerializePreview ? state.ActiveGroups : state.CommittedGroups;
            var node = root.OwnerDocument.CreateElement(Node); node.SetAttribute("version", "1");
            node.SetAttribute("tint", Number(tint.r) + " " + Number(tint.g) + " " + Number(tint.b) + " " + Number(tint.a));
            if (finish != null) { node.SetAttribute("gloss", Number(finish.Gloss)); node.SetAttribute("metallic", Number(finish.Metallic)); }
            if (groups.Count > 0)
            {
                var groupNode = root.OwnerDocument.CreateElement("Groups");
                for (int i = 0; i < GlassGroupPaint.GroupCount; i++) if (groups.TryGetValue((GlassGroup)i, out var setting))
                {
                    var group = root.OwnerDocument.CreateElement("Group"); group.SetAttribute("id", ((GlassGroup)i).ToString());
                    group.SetAttribute("tint", Number(setting.Tint.r) + " " + Number(setting.Tint.g) + " " + Number(setting.Tint.b) + " " + Number(setting.Tint.a));
                    if (setting.Finish != null) { group.SetAttribute("gloss", Number(setting.Finish.Gloss)); group.SetAttribute("metallic", Number(setting.Finish.Metallic)); }
                    groupNode.AppendChild(group);
                }
                node.AppendChild(groupNode);
            }
            var glassItems = root.OwnerDocument.CreateElement("Items");
            for (int i = 0; i < paint.items.Count; i++)
                if (GlassPrototypeState.IsGlass(paint.items[i])) glassItems.AppendChild(items.ChildNodes[i].CloneNode(true));
            for (int i = paint.items.Count - 1; i >= 0; i--)
                if (GlassPrototypeState.IsGlass(paint.items[i])) items.RemoveChild(items.ChildNodes[i]);
            if (tint.a <= .001f && finish == null && groups.Count == 0 && glassItems.ChildNodes.Count == 0) return;
            node.AppendChild(glassItems); root.AppendChild(node);
        }
        private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        internal static bool Validate(XmlElement node)
        {
            if (node == null) return true;
            if (node.GetAttribute("version") != "1" || !Values(node.GetAttribute("tint"), 4, 0, 1)) return false;
            if (node.HasAttribute("gloss") != node.HasAttribute("metallic")) return false;
            if (node.HasAttribute("gloss") && (!Values(node.GetAttribute("gloss"), 1, 0, 1) || !Values(node.GetAttribute("metallic"), 1, 0, 1))) return false;
            var groupNodes = node.SelectNodes("Groups");
            if (groupNodes.Count > 1) return false;
            if (groupNodes.Count == 1)
            {
                if (groupNodes[0].ChildNodes.Count > GlassGroupPaint.GroupCount) return false;
                int seen = 0;
                foreach (XmlNode child in groupNodes[0].ChildNodes)
                {
                    if (!(child is XmlElement group) || group.Name != "Group" || !Enum.TryParse(group.GetAttribute("id"), out GlassGroup id) ||
                        !Enum.IsDefined(typeof(GlassGroup), id) || group.GetAttribute("id") != id.ToString() || (seen & (1 << (int)id)) != 0) return false;
                    if (!Values(group.GetAttribute("tint"),4,0,1) || group.HasAttribute("gloss") != group.HasAttribute("metallic")) return false;
                    if (group.HasAttribute("gloss") && (!Values(group.GetAttribute("gloss"),1,0,1) || !Values(group.GetAttribute("metallic"),1,0,1))) return false;
                    seen |= 1 << (int)id;
                }
            }
            var items = node.SelectSingleNode("Items");
            if (items == null || items.ChildNodes.Count > 32) return false;
            foreach (XmlNode child in items.ChildNodes)
            {
                if (!(child is XmlElement item) || item.Name != "Item") return false;
                if (!Values(item.GetAttribute("pos"), 3, -100, 100) || !Values(item.GetAttribute("rot"), 3, -36000, 36000) ||
                    !Values(item.GetAttribute("size_y"), 1, .001f, 100) || !Values(item.GetAttribute("acpect"), 1, .001f, 100) ||
                    !Values(item.GetAttribute("depth"), 1, .001f, 100) || !Values(item.GetAttribute("color"), 4, 0, 1)) return false;
                var name = item.GetAttribute("sprite.name");
                if (name.Length == 0 || name.Length > 8192) return false;
                if ((name == VinylXml.TextSprite || TextVinylData.IsText(name)) && !VinylXml.TryReadText(item, out _)) return false;
                var symmetry = item.GetAttribute(VinylXml.Symmetry);
                if (symmetry.Length > 0 && symmetry != "0" && symmetry != "1") return false;
            }
            return true;
        }
        private static bool Values(string text, int count, float min, float max)
        {
            var values = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != count) return false;
            foreach (var value in values)
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                    float.IsNaN(number) || float.IsInfinity(number) || number < min || number > max) return false;
            return true;
        }
        internal static void Read(CarPaintItems paint, XmlElement root)
        {
            var state = GlassPrototypeState.Ensure(paint); state.Reset();
            var nodes = root?.SelectNodes(Node);
            if (nodes == null || nodes.Count == 0) return;
            if (nodes.Count != 1 || !(nodes[0] is XmlElement node) || !Validate(node))
            { Plugin.Warn("Invalid glass prototype XML; using original windows."); return; }
            var color = node.GetAttribute("tint").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
            var loaded = new List<CarPaintItems.Item>();
            foreach (XmlElement xml in node.SelectNodes("Items/Item"))
            {
                var item = new CarPaintItems.Item(xml);
                if (item.spriteInfo == null) { Plugin.Warn("Unknown glass vinyl: " + item.spriteName); continue; }
                GlassPrototypeState.SetGlass(item, true); loaded.Add(item);
            }
            paint.items.AddRange(loaded);
            state.Active = state.Committed = new Color(Parse(color[0]), Parse(color[1]), Parse(color[2]), Parse(color[3]));
            if (node.HasAttribute("gloss")) state.ActiveFinish = state.CommittedFinish = new GlassFinish(Parse(node.GetAttribute("gloss")), Parse(node.GetAttribute("metallic")));
            foreach (XmlElement group in node.SelectNodes("Groups/Group"))
            {
                var values = group.GetAttribute("tint").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var setting = new GlassGroupPaint(new Color(Parse(values[0]), Parse(values[1]), Parse(values[2]), Parse(values[3])),
                    group.HasAttribute("gloss") ? new GlassFinish(Parse(group.GetAttribute("gloss")), Parse(group.GetAttribute("metallic"))) : null);
                state.ActiveGroups.Add((GlassGroup)Enum.Parse(typeof(GlassGroup),group.GetAttribute("id")),setting);
            }
            state.CommittedGroups = GlassGroupPaint.Copy(state.ActiveGroups);
            state.Scan();
        }
    }
}
