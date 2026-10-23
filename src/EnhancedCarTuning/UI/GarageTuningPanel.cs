using EnhancedCarTuning.TuneUp.UI;
using EnhancedCarTuning.Appearance.UI;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using System;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Wheels.UI;
using UnityEngine;

namespace EnhancedCarTuning.UI
{
    internal sealed class GarageTuningPanel : MonoBehaviour
    {
        internal GarageSession Session { get; private set; }
        internal BodyPaintSession BodyPaintSession { get; private set; }
        internal PolicePaintSession PolicePaintSession { get; private set; }
        internal EnhancedCarTuning.Appearance.Parts.BodyPartsSession BodyPartsSession {get;private set;}
        private OtherGarageView other;
        private GarageMainMenuLayout mainLayout;
        private BodyPaintGarageView bodyPaint;
        private GlassPaintGarageView glassPaint, lampPaint;
        private BodyPaintGarageView rimPaint;
        private PaintFinish originalWheelPaint;
        internal PaintFinish WheelPaintBase => originalWheelPaint;
        internal PaintFinish RimNativeFinish => rimPaint != null && rimPaint.HasParts ? originalWheelPaint : null;
        private NativeGarageView view;
        private WheelGarageView wheels;
        private GarageAppearanceView appearance;
        private VinylGarageView vinyls;
        private CarGarageUI garage;
        private float nextRefresh;
        private bool wasVinylMenuVisible;

        internal static GarageTuningPanel Find(CarGarageUI_Buy purchase)
        {
            var ui = CarGarageUI.me;
            return ui && ReferenceEquals(ui.wasState, purchase) ? ui.GetComponent<GarageTuningPanel>() : null;
        }

        internal void Initialize(CarGarageUI ui)
        {
            Cancel();
            other?.Dispose();other=null;mainLayout?.Dispose();mainLayout=null;BodyPartsSession=null;
            lampPaint?.Dispose(); lampPaint = null;
            rimPaint?.Dispose(); rimPaint = null;
            glassPaint?.Dispose(); glassPaint = null;
            bodyPaint?.Dispose(); bodyPaint = null; BodyPaintSession = null;
            view?.Dispose();
            view = null;
            wheels?.Dispose(); wheels = null;
            vinyls?.Dispose(); vinyls = null;
            appearance?.Dispose(); appearance = null;
            PolicePaintSession = null;
            garage = ui;
            wasVinylMenuVisible = false;
            var state = VehicleTuningState.Ensure(ui.car);
            if (!state) return;
            state.RestoreNativeTuning();
            Session = new GarageSession(state);
            originalWheelPaint = PaintFinish.Capture(ui.carMat.wheel);
            var police = PolicePaintState.Ensure(ui.carMat);
            if (police && police.Supported) PolicePaintSession = new PolicePaintSession(police, Session);
            try { appearance = new GarageAppearanceView(ui, PolicePaintSession); appearance.Build(); }
            catch (Exception error) { appearance?.Dispose(); appearance = null; Plugin.Warn("Cannot split paint/vinyl menus: " + error); }
            try
            {
                var paintState = BodyPaintState.Ensure(ui.carMat);
                if (paintState && appearance != null)
                {
                    BodyPaintSession = new BodyPaintSession(paintState, Session);
                    bodyPaint = new BodyPaintGarageView(ui, BodyPaintSession); bodyPaint.Build();
                }
            }
            catch (Exception error)
            { bodyPaint?.Dispose(); bodyPaint = null; BodyPaintSession?.Cancel(); BodyPaintSession = null; Plugin.Warn("Cannot attach body-part paint: " + error); }
            try
            {
                var glass = GetComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>() ?? gameObject.AddComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>();
                glass.Initialize(ui);
                if (appearance != null)
                {
                    glassPaint = new GlassPaintGarageView(ui, glass); glassPaint.Build();
                    lampPaint = new GlassPaintGarageView(ui, glass, true, glassPaint.Entry); lampPaint.Build();
                    appearance.SetGlassEntry(lampPaint.HasParts ? lampPaint.Entry : glassPaint.Entry);
                }
            }
            catch (Exception error)
            { lampPaint?.Dispose(); lampPaint = null; glassPaint?.Dispose(); glassPaint = null; Plugin.Warn("Cannot attach glass paint to the native garage: " + error); }
            try { vinyls = new VinylGarageView(ui); vinyls.Build(); }
            catch (Exception error) { vinyls?.Dispose(); vinyls = null; Plugin.Warn("Cannot attach text/symmetry vinyl editor: " + error); }
            try
            {
                view = new NativeGarageView(ui, Session);
                view.Build();
            }
            catch (Exception error)
            {
                view?.Dispose();
                view = null;
                Plugin.Warn("Cannot attach TUNE-UP to the native garage: " + error);
            }
            try { wheels = new WheelGarageView(ui, Session); wheels.Build(); }
            catch (Exception error) { wheels?.Dispose(); wheels = null; Plugin.Warn("Cannot attach wheel tuning: " + error); }
            if (wheels != null) try
            { rimPaint = new BodyPaintGarageView(ui, PartPaintBinding.Rims(ui, Session, () => wheels.SelectedAxle)); rimPaint.Build(); }
            catch (Exception error) { rimPaint?.Dispose(); rimPaint = null; Plugin.Warn("Cannot attach rim part paint: " + error); }
            try
            {
                BodyPartsSession=new EnhancedCarTuning.Appearance.Parts.BodyPartsSession(EnhancedCarTuning.Appearance.Parts.BodyPartsState.Ensure(ui.car),Session);
                other=new OtherGarageView(ui,BodyPartsSession,PolicePaintSession);other.Build();
                mainLayout=new GarageMainMenuLayout(ui);mainLayout.Build();
            }
            catch(Exception error){other?.Dispose();other=null;mainLayout?.Dispose();mainLayout=null;Plugin.Warn("Cannot attach body parts menu: "+error);}
        }

        private void Update()
        {
            if (!garage) return;
            view?.SetVisible(EnhancedCarTuning.Configuration.TuningConfig.ShowGarageButton);
            wheels?.Layout();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.15f;
            view?.LayoutLists();
            view?.Refresh();
            wheels?.Refresh();
            appearance?.RefreshPoliceButton();
            mainLayout?.Refresh();
            NativeMenuFactory.RefreshLocalizedLabels(garage.transform);
        }

        internal string NativeTitle => other!=null&&other.IsOpen?EnhancedCarTuning.Localization.ECTLocalization.T("ДЕТАЛИ КУЗОВА"):lampPaint != null && lampPaint.IsOpen ? EnhancedCarTuning.Localization.ECTLocalization.T("ПОКРАСКА ФАР") :
            rimPaint != null && rimPaint.HasParts && rimPaint.IsOpen ? EnhancedCarTuning.Localization.ECTLocalization.T("ПОКРАСКА ДИСКОВ") : glassPaint != null && glassPaint.IsOpen ? EnhancedCarTuning.Localization.ECTLocalization.T("ПОКРАСКА СТЕКОЛ") :
            bodyPaint != null && bodyPaint.IsOpen ? EnhancedCarTuning.Localization.ECTLocalization.T("ПОКРАСКА КУЗОВА") :
            appearance?.Title ?? (view != null && view.IsActive ? "TUNE-UP" : null);
        private void LateUpdate()
        {
            if (!garage || !garage.carMat) return;
            bool visible = garage && garage.root && garage.root.Find("PaintVinils") &&
                garage.root.Find("PaintVinils").gameObject.activeInHierarchy;
            if (visible && !wasVinylMenuVisible && garage.carPaint != null &&
                garage.editItem == null && garage.carPaint.items.Count > 0)
                garage.PaintOnShow();
            wasVinylMenuVisible = visible;
            if (rimPaint == null || !rimPaint.HasParts)
            {
                var native = garage.carMat.wheel;
                if (originalWheelPaint == null || !originalWheelPaint.SameColor(native.color) ||
                    Math.Abs(originalWheelPaint.Gloss-native.Gloss) > .001f || Math.Abs(originalWheelPaint.Metallic-native.Mettalic) > .001f)
                    originalWheelPaint = PaintFinish.Capture(native);
            }
            vinyls?.Refresh(); bodyPaint?.Refresh(); rimPaint?.Refresh(); glassPaint?.Refresh(); lampPaint?.Refresh();
            other?.Refresh();mainLayout?.Refresh();
            appearance?.SetGlassEntry(lampPaint != null && lampPaint.HasParts ? lampPaint.Entry : glassPaint != null && glassPaint.HasParts ? glassPaint.Entry : null);
            appearance?.RefreshPaintTexture();
        }
        internal bool IsCameraInputArea(Vector2 position) => ColorPickerHex.ContainsPointer(position)||other!=null&&other.ContainsPointer()||lampPaint != null && lampPaint.ContainsPointer() || rimPaint != null && rimPaint.ContainsPointer() || glassPaint != null && glassPaint.ContainsPointer() || wheels != null && wheels.IsCameraInputArea(position);
        internal bool TryCollapseTuningList(MenuButtonGroup button, bool isMouseClick) =>
            view != null && view.TryCollapseList(button, isMouseClick);
        internal void FlushVinyls() => vinyls?.Flush();
        internal void FlushBodyPaint() { bodyPaint?.FlushPicker(); rimPaint?.FlushPicker(); glassPaint?.FlushPicker(); lampPaint?.FlushPicker(); }
        internal void CloseGlassEditors(GlassPaintGarageView keep)
        { if (!ReferenceEquals(keep, glassPaint)) glassPaint?.CloseEditor(); if (!ReferenceEquals(keep, lampPaint)) lampPaint?.CloseEditor(); }
        internal void RefreshImportedBodyPaint(bool preserveOther = false) { if (!preserveOther) BodyPartsSession?.RebaseImported();originalWheelPaint = PaintFinish.Capture(garage.carMat.wheel); bodyPaint?.SyncImported(); rimPaint?.SyncImported(); glassPaint?.SyncImported(); lampPaint?.SyncImported(); }
        internal bool IsTypingVinyl => ColorPickerHex.AnyTyping||vinyls != null && vinyls.IsTyping;
        internal bool IsBodyPaint => appearance != null && appearance.IsColors;
        internal bool ContainsPointer() => ColorPickerHex.ContainsPointer(Input.mousePosition)||other!=null&&other.ContainsPointer()||lampPaint != null && lampPaint.ContainsPointer() || rimPaint != null && rimPaint.ContainsPointer() || glassPaint != null && glassPaint.ContainsPointer() || bodyPaint != null && bodyPaint.ContainsPointer() || view != null && view.ContainsPointer() || wheels != null && wheels.ContainsPointer() || vinyls != null && vinyls.ContainsPointer();
        internal void Cancel() { BodyPartsSession?.Cancel();lampPaint?.CloseEditor(); glassPaint?.CloseEditor(); GetComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>()?.Cancel(); PolicePaintSession?.Cancel(); BodyPaintSession?.Cancel(); Session?.Cancel(); }
        private void OnDestroy() { other?.Dispose();other=null;mainLayout?.Dispose();mainLayout=null;lampPaint?.Dispose(); lampPaint = null; rimPaint?.Dispose(); rimPaint = null; glassPaint?.Dispose(); glassPaint = null; Cancel(); bodyPaint?.Dispose(); bodyPaint = null; wheels?.Dispose(); wheels = null; view?.Dispose(); view = null; vinyls?.Dispose(); vinyls = null; appearance?.Dispose(); appearance = null; }
    }
}
