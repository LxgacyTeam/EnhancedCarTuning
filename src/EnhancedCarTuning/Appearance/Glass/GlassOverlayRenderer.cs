using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal static class GlassOverlayRenderer
    {
        private static readonly HashSet<GlassPrototypeState> states = new HashSet<GlassPrototypeState>();
        private static readonly Dictionary<Camera, CommandBuffer> buffers = new Dictionary<Camera, CommandBuffer>();
        private static readonly List<Camera> destroyed = new List<Camera>();
        private static bool subscribed;
        internal static void Register(GlassPrototypeState state)
        {
            if (NetManager.isServer) return;
            states.Add(state);
            if (subscribed) return;
            Camera.onPreCull += Prepare;
            subscribed = true;
        }
        internal static void Unregister(GlassPrototypeState state)
        { states.Remove(state); if (states.Count == 0) Clear(); }
        private static void Prepare(Camera camera)
        {
            if (!camera) return;
            if (camera.cullingMask == 0) { if (buffers.TryGetValue(camera, out var empty)) empty.Clear(); return; }
            destroyed.Clear();
            foreach (var entry in buffers) if (!entry.Key) { entry.Value.Release(); destroyed.Add(entry.Key); }
            foreach (var old in destroyed) buffers.Remove(old);
            if (!buffers.TryGetValue(camera, out var commands))
            {
                commands = new CommandBuffer { name = "ECT.Glass.TintAndVinyls" };
                buffers.Add(camera, commands);
                camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
            }
            commands.Clear();
            foreach (var state in states) if (state && state.isActiveAndEnabled) state.RenderOverlay(camera, commands);
        }
        internal static void Clear()
        {
            if (subscribed) Camera.onPreCull -= Prepare;
            subscribed = false;
            foreach (var entry in buffers)
            {
                if (entry.Key) entry.Key.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, entry.Value);
                entry.Value.Release();
            }
            buffers.Clear(); states.Clear(); destroyed.Clear();
        }
    }
}
