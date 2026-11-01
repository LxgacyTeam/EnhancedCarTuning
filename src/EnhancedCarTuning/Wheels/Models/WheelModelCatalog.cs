using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnhancedCarTuning.Wheels.Models
{
    internal sealed class WheelModelPart
    {
        internal Mesh Mesh;
        internal Material[] Materials;
        internal bool[] Paintable;
    }
    internal sealed class WheelModelOption
    {
        internal string Id, LeftName, RightName;
        internal string[] Donors;
        internal bool RearVariant;
        internal WheelModelPart Left, Right;
        internal WheelModelOption() { }
        internal WheelModelOption(string id, string left, string right, bool rearVariant, params string[] donors)
        { Id = id; LeftName = left; RightName = right; RearVariant = rearVariant; Donors = donors; }
        internal WheelModelPart For(bool left) => left ? Left : Right;
        internal bool IsRim => Id.StartsWith("rim_", StringComparison.Ordinal);
        internal bool Ready => Left != null && Right != null && Left.Mesh && Right.Mesh;
    }
    internal static class WheelModelCatalog
    {
        private static readonly Dictionary<string, WheelModelOption> legacy = new Dictionary<string, WheelModelOption>(StringComparer.Ordinal);
        internal static IReadOnlyList<WheelModelOption> Options
        {
            get
            {
                var array = ControlArray.byType(ControlArray.Type.Car);
                return WheelModelDefinitions.All.Where(o => o.IsRim || array && array.controls.Any(i => o.Donors.Contains(i.name))).ToArray();
            }
        }
        internal static bool IsKnown(string id)
        {
            if (id == "stock") return true;
            if (!WheelModelXml.ValidId(id)) return false;
            var definition = Array.Find(WheelModelDefinitions.All, o => o.Id == id);
            if (definition != null && definition.IsRim) return true;
            var array = ControlArray.byType(ControlArray.Type.Car);
            if (!array) return false;
            return definition != null ? array.controls.Any(i => definition.Donors.Contains(i.name)) : array.controls.Any(i => i.name == id);
        }
        internal static bool TryGet(string id, out WheelModelOption option)
        {
            option = Array.Find(WheelModelDefinitions.All, o => o.Id == id);
            if (option != null && option.Ready) return true;
            if (option == null && legacy.TryGetValue(id, out option) && option.Ready) return true;
            var definition = Array.Find(WheelModelDefinitions.All, o => o.Id == id);
            if (definition != null && definition.IsRim) { option = null; return false; }
            var array = ControlArray.byType(ControlArray.Type.Car);
            if (!array) return false;
            foreach (var item in array.controls)
            {
                if (definition != null ? !definition.Donors.Contains(item.name) : item.name != id) continue;
                if (!(item.control is CarControl car)) continue;
                if (!CapturePair(car, definition, out var left, out var right)) continue;
                option = definition ?? new WheelModelOption { Id = id };
                option.Left = left; option.Right = right;
                if (definition == null) legacy[id] = option;
                return true;
            }
            foreach (var item in array.controls)
                if ((definition != null ? definition.Donors.Contains(item.name) : item.name == id) && !item.control)
                { item.LoadControl(); break; }
            option = null; return false;
        }
        private static bool CapturePair(CarControl car, WheelModelOption definition, out WheelModelPart leftPart, out WheelModelPart rightPart)
        {
            leftPart = rightPart = null;
            CarWheel left = null, right = null;
            foreach (var wheel in car.GetComponentsInChildren<CarWheel>(true))
            {
                var filter = wheel.GetComponent<MeshFilter>(); var renderer = wheel.GetComponent<MeshRenderer>();
                if (!filter || !filter.sharedMesh || !renderer || renderer.sharedMaterials.Length == 0 ||
                    !WheelModelGeometry.Valid(filter.sharedMesh.bounds.size)) continue;
                bool isLeft = wheel.transform.localPosition.x < 0;
                if (definition != null && filter.sharedMesh.name != (isLeft ? definition.LeftName : definition.RightName)) continue;
                if (isLeft) { if (!left || wheel.transform.localPosition.z > left.transform.localPosition.z) left = wheel; }
                else if (!right || wheel.transform.localPosition.z > right.transform.localPosition.z) right = wheel;
            }
            if (!left || !right) return false;
            var material = car.GetComponent<CarMaterial>();
            leftPart = Capture(left, material); rightPart = Capture(right, material); return true;
        }
        private static WheelModelPart Capture(CarWheel wheel, CarMaterial car)
        {
            var materials = wheel.GetComponent<MeshRenderer>().sharedMaterials;
            var paint = new bool[materials.Length];
            for (int i = 0; i < materials.Length; i++)
                paint[i] = materials[i] && (car && car.wheel != null && (materials[i] == car.wheel.mat || materials[i] == car.wheel.usedMatreial) ||
                    materials[i].shader && materials[i].shader.name.StartsWith("Custom/Car/CarWheel", StringComparison.Ordinal));
            return new WheelModelPart { Mesh = wheel.GetComponent<MeshFilter>().sharedMesh, Materials = materials, Paintable = paint };
        }
        internal static string Label(string id)
        {
            if (id == "stock") return "Штатные";
            var definition = Array.Find(WheelModelDefinitions.All, o => o.Id == id);
            if (definition != null) return definition.IsRim ? definition.RightName.Substring(0, definition.RightName.Length - 2) : definition.RightName;
            var front = Array.Find(WheelModelDefinitions.All, o => !o.RearVariant && o.Donors.Contains(id));
            if (front != null) return front.RightName;
            if (TryGet(id, out var option)) return option.Right.Mesh.name;
            return id;
        }
        internal static int IndexOf(string id, IReadOnlyList<WheelModelOption> options)
        {
            if (id == "stock") return 0;
            for (int i = 0; i < options.Count; i++) if (options[i].Id == id) return i + 1;
            for (int i = 0; i < options.Count; i++)
                if (!options[i].RearVariant && options[i].Donors.Contains(id)) return i + 1;
            return 0;
        }
        internal static string Step(string id, int direction)
        {
            var options = Options;
            int index = WheelModelCycle.Step(IndexOf(id, options), options.Count + 1, direction);
            return index == 0 ? "stock" : options[index - 1].Id;
        }
        internal static void Reset()
        {
            legacy.Clear();
            foreach (var definition in WheelModelDefinitions.All) definition.Left = definition.Right = null;
        }
    }
}
