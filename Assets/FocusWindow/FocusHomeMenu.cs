using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LockedIn
{
    internal enum BackgroundStyle
    {
        Passthrough,
        Dimmed,
        RoomBlur,
        ShanghaiBund,
        Library,
        Beach,
        SpaceStars,
        SundownerOverlook,
        SecludedBeach
    }

    internal sealed partial class FocusHomeMenu : IDisposable
    {
        private static readonly Color Panel = new Color32(15, 22, 32, 255);
        private static readonly Color Card = new Color32(31, 44, 59, 255);
        private static readonly Color Selected = new Color32(37, 81, 78, 255);
        private static readonly Color Highlight = new Color32(47, 107, 101, 255);
        private static readonly Color Accent = new Color32(124, 247, 209, 255);
        private static readonly Color Ink = new Color32(239, 245, 249, 255);
        private static readonly Color Muted = new Color32(176, 195, 209, 255);

        private readonly RectTransform panel;
        private readonly RectTransform home;
        private readonly RectTransform backgrounds;
        private readonly RectTransform cursor;
        private readonly Font font;
        private readonly RectTransform calibration;
        private readonly Text calibrationTitle;
        private readonly Text calibrationHint;
        private readonly RectTransform knobStatus;
        private readonly Text knobStatusText;
        private bool calibrationActive;
        private readonly List<MenuItem> items = new List<MenuItem>();

        public bool IsOpen => panel.gameObject.activeSelf;

        public FocusHomeMenu(Transform camera, Action<BackgroundStyle> choose, Action createBorder, Action autoSelect, Action detectWorkspace)
        {
            panel = CreateRect("LockedIn menu", camera, new Vector2(640, 450), Vector2.zero);
            panel.localPosition = new Vector3(0f, -0.025f, 1.1f);
            panel.localScale = Vector3.one * 0.0011f;
            var canvas = panel.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera.GetComponent<Camera>();
            canvas.sortingOrder = 5;
            AddImage(panel, Panel);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            AddText(panel, "LockedIn", new Vector2(556, 58), new Vector2(0, 169), 46, Ink, FontStyle.Bold);
            AddText(panel, "Right trigger to select  •  Right grip to close", new Vector2(600, 36),
                    new Vector2(0, -193), 18, Muted, alignment: TextAnchor.MiddleCenter);

            home = CreateRect("Home", panel, panel.sizeDelta, Vector2.zero);
            backgrounds = CreateRect("Choose Background", panel, panel.sizeDelta, Vector2.zero);

            AddItem(home, "Detect Workspace (AI)", new Vector2(556, 55), new Vector2(0, 100), () => ExplainAI(detectWorkspace));
            AddItem(home, "Auto Select Screen", new Vector2(556, 55), new Vector2(0, 36), () => ExplainScreen(autoSelect));
            AddItem(home, "Create Border", new Vector2(556, 55), new Vector2(0, -28), () => ExplainManual(createBorder));
            AddItem(home, "Choose Background", new Vector2(556, 55), new Vector2(0, -92),
                    () => ExplainBackgrounds());

            AddItem(home, "How to use LockedIn", new Vector2(556, 42), new Vector2(0, -148), StartWalkthrough);

            AddText(backgrounds, "Choose Background", new Vector2(556, 36), new Vector2(0, 110),
                    26, Muted, FontStyle.Bold);
            var styles = new[] { BackgroundStyle.SundownerOverlook, BackgroundStyle.SecludedBeach,
                BackgroundStyle.RoomBlur, BackgroundStyle.ShanghaiBund, BackgroundStyle.Library,
                BackgroundStyle.Beach, BackgroundStyle.SpaceStars };
            var names = new[] { "Sundowner Overlook", "Secluded Beach", "Room Blur", "Shanghai Bund",
                "Historic Library", "Sunrise Beach", "Deep Space" };
            for (int i = 0; i < styles.Length; i++)
            {
                var choice = styles[i];
                AddItem(backgrounds, names[i], new Vector2(268, 48),
                    new Vector2(i % 2 == 0 ? -144 : 144, 64 - (i / 2) * 54),
                    () => choose(choice), choice);
            }
            AddItem(backgrounds, "Back", new Vector2(268, 48), new Vector2(144, -98),
                    () => ShowBackgrounds(false));
            AddText(backgrounds, "Library: Waldyrious / CC BY-SA 4.0 | Beaches/city: Poly Haven / CC0\nDeep Space: NASA/Goddard Scientific Visualization Studio",
                new Vector2(590, 42), new Vector2(0, -151), 13, Muted,
                alignment: TextAnchor.MiddleCenter);

            cursor = CreateRect("Controller pointer", panel, new Vector2(12, 12), Vector2.zero);
            cursor.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(cursor, Accent);
            cursor.gameObject.SetActive(false);
            ShowBackgrounds(false);

            calibration = CreateRect("Border calibration", camera, new Vector2(760, 170), Vector2.zero);
            calibration.localPosition = new Vector3(0f, -0.25f, 1.1f);
            calibration.localScale = Vector3.one * 0.0011f;
            var calibrationCanvas = calibration.gameObject.AddComponent<Canvas>();
            calibrationCanvas.renderMode = RenderMode.WorldSpace;
            calibrationCanvas.worldCamera = canvas.worldCamera;
            calibrationCanvas.sortingOrder = canvas.sortingOrder;
            AddImage(calibration, Panel);
            calibrationTitle = AddText(calibration, "", new Vector2(710, 58), new Vector2(0, 43),
                                       30, Ink, FontStyle.Bold);
            calibrationHint = AddText(calibration, "", new Vector2(710, 88), new Vector2(0, -29), 22, Muted);
            calibration.gameObject.SetActive(false);
            knobStatus = CreateRect("Environment effect status", camera, new Vector2(620, 64), Vector2.zero);
            knobStatus.localPosition = new Vector3(0f, -0.30f, 0.8f);
            knobStatus.localScale = Vector3.one * 0.00055f;
            var statusCanvas = knobStatus.gameObject.AddComponent<Canvas>();
            statusCanvas.renderMode = RenderMode.WorldSpace;
            statusCanvas.worldCamera = canvas.worldCamera;
            statusCanvas.sortingOrder = canvas.sortingOrder + 1;
            knobStatusText = AddText(knobStatus, "Environment 0% | NO SIGNAL - CLEAR", knobStatus.sizeDelta,
                Vector2.zero, 20, Muted, alignment: TextAnchor.MiddleCenter);
            BuildInstructions();
            if (PlayerPrefs.GetInt("LockedIn.Walkthrough.v2", 0) == 0) StartWalkthrough();
        }

        public void ShowCalibration(string title, string hint)
        {
            calibrationTitle.text = title;
            calibrationHint.text = hint;
            calibrationActive = true;
            SetCalibrationVisible(true);
        }

        public void SetKnobStatus(string value) => knobStatusText.text = value;

        public void HideCalibration()
        {
            calibrationActive = false;
            SetCalibrationVisible(false);
        }

        public void SetCalibrationVisible(bool visible)
        {
            calibration.gameObject.SetActive(visible && calibrationActive && !IsOpen);
        }

        public void Toggle(BackgroundStyle selected)
        {
            if (IsOpen)
            {
                Hide();
                return;
            }

            ShowBackgrounds(false);
            panel.gameObject.SetActive(true);
            HighlightItem(null, selected);
        }

        public void Hide()
        {
            cursor.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
        }

        public void UpdatePointer(bool hasRay, Ray ray, bool pressed, BackgroundStyle selected)
        {
            MenuItem hovered = null;
            bool showCursor = false;
            var plane = new Plane(panel.forward, panel.position);
            if (hasRay && plane.Raycast(ray, out float distance))
            {
                Vector3 hit = ray.GetPoint(distance);
                Vector2 local = panel.InverseTransformPoint(hit);
                showCursor = panel.rect.Contains(local);
                if (showCursor)
                {
                    cursor.anchoredPosition = local;
                    foreach (MenuItem item in items)
                    {
                        RectTransform rect = item.Surface.rectTransform;
                        if (rect.gameObject.activeInHierarchy && rect.rect.Contains((Vector2)rect.InverseTransformPoint(hit)))
                        {
                            hovered = item;
                            break;
                        }
                    }
                }
            }

            cursor.gameObject.SetActive(showCursor);
            HighlightItem(hovered, selected);
            if (pressed)
                hovered?.Activate();
        }

        public void Dispose()
        {
            if (panel != null)
                UnityEngine.Object.Destroy(panel.gameObject);
            if (calibration != null)
                UnityEngine.Object.Destroy(calibration.gameObject);
            if (knobStatus != null)
                UnityEngine.Object.Destroy(knobStatus.gameObject);
        }

        private void ShowBackgrounds(bool show)
        {
            if (instructions != null) instructions.gameObject.SetActive(false);
            home.gameObject.SetActive(!show);
            backgrounds.gameObject.SetActive(show);
        }

        private void HighlightItem(MenuItem hovered, BackgroundStyle selected)
        {
            foreach (MenuItem item in items)
            {
                bool isSelected = item.Style == selected;
                item.Surface.color = item == hovered ? Highlight : isSelected ? Selected : Card;
                if (item.Style.HasValue)
                    item.Label.text = isSelected ? item.Title + "  *" : item.Title;
            }
        }

        private void AddItem(RectTransform parent, string title, Vector2 size, Vector2 position, Action activate,
                             BackgroundStyle? style = null)
        {
            RectTransform rect = CreateRect(title, parent, size, position);
            Image surface = AddImage(rect, Card);
            Text label = AddText(rect, title, size - new Vector2(32, 0), Vector2.zero, 28, Ink, FontStyle.Bold);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = 28;
            items.Add(new MenuItem(title, surface, label, activate, style));
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text AddText(RectTransform parent, string value, Vector2 size, Vector2 position, int fontSize,
                             Color color, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = CreateRect(value, parent, size, position).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private sealed class MenuItem
        {
            public readonly string Title;
            public readonly Image Surface;
            public readonly Text Label;
            public readonly Action Activate;
            public readonly BackgroundStyle? Style;

            public MenuItem(string title, Image surface, Text label, Action activate, BackgroundStyle? style)
            {
                Title = title;
                Surface = surface;
                Label = label;
                Activate = activate;
                Style = style;
            }
        }
    }
}
