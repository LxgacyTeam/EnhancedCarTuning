using System;
using System.Globalization;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.Wheels.Suspension;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EnhancedCarTuning.Wheels.UI
{
    // Native slider artwork, with input owned by the individual control rather than lastCreate.
    internal sealed class WheelSlider : MonoBehaviour
    {
        private static WheelSlider focused;
        private MenuSliderPanel panel;
        private GarageSession session;
        private Func<AxleTarget> axle;
        private WheelParameter parameter;
        private string title, unit;
        private bool dragging, editable;
        private float current;
        private float minimum, maximum;

        internal void Initialize(MenuSliderPanel native, GarageSession owner, Func<AxleTarget> selected, WheelParameter kind, string label, string suffix, Font font)
        {
            panel = native; session = owner; axle = selected; parameter = kind; title = label; unit = suffix;
            panel.enabled = false;
            panel.valueChangeEvent = new UnityEvent(); panel.StepFilter = null; panel.moveByStep = 0;
            foreach (var localize in GetComponentsInChildren<Localize>(true)) { localize.enabled = false; localize.isInit = true; }
            panel.L.gameObject.SetActive(false); panel.R.gameObject.SetActive(false);
            foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (panel.panel.transform != transform)
            {
                panel.panel.transform.SetParent(transform, false);
                WheelGarageView.Fit(panel.panel.rectTransform, 0, 0, 1, 1);
            }
            panel.panel.color = new Color(0.22f, 0.27f, 0.29f, 0.88f);
            WheelControlLabels.Format(panel.text.ui, transform, font, false);
            WheelControlLabels.Format(panel.value, transform, font, true);
            panel.text.ui.text = ECTLocalization.T(title);
            panel.hRoot.transform.SetParent(transform, false);
            WheelGarageView.Fit(panel.hRoot.rectTransform, 0.07f, 0.23f, 0.93f, 0.27f);
            panel.hRoot.color = new Color(0.12f, 0.17f, 0.19f, 1);
            panel.hPoint.transform.SetParent(panel.hRoot.transform, false);
            var thumb = panel.hPoint.rectTransform;
            thumb.localScale = Vector3.one; thumb.localRotation = Quaternion.identity;
            thumb.anchorMin = thumb.anchorMax = thumb.pivot = new Vector2(0.5f, 0.5f);
            thumb.sizeDelta = new Vector2(22, 22);
            panel.hPoint.color = new Color(0.82f, 0.85f, 0.86f, 1);
            Refresh();
        }

        internal void Refresh()
        {
            bool found = session.State.WheelService.Read(session.Selection.Draft.Wheels, axle(), parameter, out current, out bool mixed);
            session.State.WheelService.Range(axle(), parameter, out minimum, out maximum);
            editable = found && session.CanEdit && CanReceiveInput();
            panel.text.ui.text = ECTLocalization.T(title);
            float shown = parameter == WheelParameter.Radius ? current * 2 : current;
            panel.value.text = !found ? "—" : mixed ? ECTLocalization.T("Разные") : shown.ToString("0.#", CultureInfo.InvariantCulture) + ECTLocalization.T(unit);
            float t = Mathf.InverseLerp(minimum, maximum, current);
            var thumb = panel.hPoint.rectTransform;
            thumb.anchorMin = thumb.anchorMax = new Vector2(t, 0.5f); thumb.anchoredPosition = Vector2.zero;
            panel.panel.color = new Color(0.22f, 0.27f, 0.29f, editable ? 0.88f : 0.45f);
        }

        private void Update()
        {
            if (!editable || !session.CanEdit || !CanReceiveInput()) { dragging = false; return; }
            if (Input.GetMouseButtonDown(0))
            {
                dragging = ((RectTransform)transform).isMouseIntersect(Input.mousePosition);
                if (dragging) focused = this;
                else if (focused == this) focused = null;
            }
            if (!Input.GetMouseButton(0)) dragging = false;
            if (dragging)
            {
                Vector2 pos = Vector2.zero;
                panel.hRoot.rectTransform.MouseOrHead_LocalPos(ref pos);
                Set(Mathf.Round(Mathf.Lerp(minimum, maximum, Mathf.Clamp01(pos.x))));
            }
            else if (focused == this)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) Set(current - 1);
                if (Input.GetKeyDown(KeyCode.RightArrow)) Set(current + 1);
            }
        }

        private void Set(float value)
        {
            value = Mathf.Clamp(value, minimum, maximum);
            session.SelectWheel(axle(), parameter, value); Refresh();
        }
        private void OnDisable() { dragging = false; if (focused == this) focused = null; }
        private static bool CanReceiveInput() => CarGarageUI.me && CarGarageUI.me.top_panel &&
            CarGarageUI.me.top_panel.cur == CarGarageUI.me && !MenuBuy.me;
    }
}
