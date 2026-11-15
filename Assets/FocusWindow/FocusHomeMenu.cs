using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace LockedIn
{
    internal enum BackgroundStyle
    {
        Passthrough, Dimmed, RoomBlur, ShanghaiBund, Library, Beach, SpaceStars,
        SundownerOverlook, SecludedBeach
    }

    internal sealed partial class FocusHomeMenu : IDisposable
    {
        private static readonly Color Panel = new Color32(239, 237, 229, 255);
        private static readonly Color Card = new Color32(226, 225, 215, 255);
        private static readonly Color Selected = new Color32(210, 221, 208, 255);
        private static readonly Color Highlight = new Color32(250, 248, 241, 255);
        private static readonly Color Accent = new Color32(206, 69, 32, 255);
        private static readonly Color Ink = new Color32(32, 43, 36, 255);
        private static readonly Color Muted = new Color32(96, 108, 98, 255);
        private static readonly Color Line = new Color32(199, 204, 191, 255);
        private readonly RectTransform panel, home, backgrounds, cursor;
        private readonly Font font, headingFont;
        private readonly RectTransform calibration;
        private readonly Text calibrationTitle, calibrationHint;
        private readonly Text dialValue, dialState, workspaceState, effectDetail;
        private readonly FocusDialGraphic dial;
        private readonly CanvasGroup pageFade;
        private readonly FocusUiMotion motion;
        private readonly RawImage homePhoto, galleryPhoto;
        private readonly Text homePhotoTitle, galleryTitle, galleryCaption;
        private readonly Text galleryPlaceholder;
        private readonly Dictionary<BackgroundStyle, Texture2D> previews = new Dictionary<BackgroundStyle, Texture2D>();
        private readonly List<MenuItem> items = new List<MenuItem>();
        private bool calibrationActive;
        private float targetAmount, shownAmount, pressPulse;
        private int displayedPercent = -1;
        private BackgroundStyle currentStyle = BackgroundStyle.Dimmed;
        private BackgroundStyle previewStyle = BackgroundStyle.SecludedBeach;
        private MenuItem lastHovered;
        public bool IsOpen => panel.gameObject.activeSelf;

        private static readonly BackgroundStyle[] Styles = { BackgroundStyle.SundownerOverlook,
            BackgroundStyle.SecludedBeach, BackgroundStyle.RoomBlur, BackgroundStyle.ShanghaiBund,
            BackgroundStyle.Library, BackgroundStyle.Beach, BackgroundStyle.SpaceStars };
        private static readonly string[] Names = { "Sundowner Overlook", "Secluded Beach", "Room Blur",
            "Shanghai Bund", "Historic Library", "Sunrise Beach", "Deep Space" };
        private static readonly string[] Captions = { "A last light escape / Poly Haven", "A quieter coast / Poly Haven",
            "Your room, softened / Live camera", "After dark on the waterfront / Poly Haven",
            "Room to think / Waldyrious", "First light, open water / Poly Haven", "A different perspective / NASA SVS" };
        private static readonly string[] ResourcesNames = { "sundowner_overlook", "secluded_beach", "", "shanghai_bund", "library", "beach", "space_stars" };

        public FocusHomeMenu(Transform camera, Action<BackgroundStyle> choose, Action createBorder,
            Action autoSelect, Action detectWorkspace, Action debugWorkspace = null)
        {
            font = Resources.Load<Font>("LockedIn/Interface/Fonts/Barlow-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headingFont = Resources.Load<Font>("LockedIn/Interface/Fonts/BarlowCondensed-SemiBold") ?? font;
            panel = CreateRect("LockedIn / focus console", camera, new Vector2(960, 660), Vector2.zero);
            panel.localPosition = new Vector3(0, -0.015f, 1.25f);
            panel.localScale = Vector3.one * 0.00095f;
            SetupCanvas(panel, camera.GetComponent<Camera>(), 5);
            AddImage(panel, Panel);
            // Printed spine and a simple, proprietary corner mark, built from geometry.
            Rule(panel, new Vector2(-462, 0), new Vector2(4, 660), Accent);
            Mark(panel, new Vector2(-414, 278), 28);
            Heading(panel, "LOCKEDIN", new Vector2(230, 42), new Vector2(-263, 280), 36);
            AddText(panel, "A LITTLE LESS WORLD. A LITTLE MORE FOCUS.", new Vector2(440, 30), new Vector2(210, 278), 16, Muted, alignment: TextAnchor.MiddleRight);
            Rule(panel, new Vector2(0, 245), new Vector2(864, 1), Line);
            Rule(panel, new Vector2(0, -258), new Vector2(864, 1), Line);
            AddText(panel, "RIGHT TRIGGER   Select", new Vector2(250, 30), new Vector2(-307, -291), 18, Ink);
            AddText(panel, "RIGHT GRIP   Home / return to room", new Vector2(390, 30), new Vector2(237, -291), 18, Muted, alignment: TextAnchor.MiddleRight);
            var pages = CreateRect("Pages", panel, panel.sizeDelta, Vector2.zero);
            pageFade = pages.gameObject.AddComponent<CanvasGroup>();
            home = CreateRect("Study console", pages, panel.sizeDelta, Vector2.zero);
            backgrounds = CreateRect("Environment collection", pages, panel.sizeDelta, Vector2.zero);
            AddText(home, "01 / WORKSPACE", new Vector2(470, 26), new Vector2(-197, 211), 18, Accent);
            Heading(home, "Set your\nstudy space.", new Vector2(490, 160), new Vector2(-187, 115), 68);
            AddText(home, "Keep your screen clear. Leave the rest behind.", new Vector2(490, 35), new Vector2(-187, 13), 21, Muted);
            AddAction(home, "01", "Detect workspace", "Find your screen + keyboard with AI", new Vector2(-187, -57), () => ExplainAI(detectWorkspace), true);
            AddAction(home, "02", "Find screen edges", "Select a screen using depth", new Vector2(-187, -135), () => ExplainScreen(autoSelect));
            AddAction(home, "03", "Place a border", "Mark three corners by hand", new Vector2(-187, -213), () => ExplainManual(createBorder));
            Rule(home, new Vector2(95, -1), new Vector2(1, 444), Line);
            AddText(home, "02 / SURROUNDINGS", new Vector2(300, 26), new Vector2(282, 211), 18, Accent);
            var photoButton = CreateRect("Browse environments", home, new Vector2(300, 158), new Vector2(282, 104));
            var photoSurface = AddImage(photoButton, Card);
            homePhoto = Photo(photoButton, photoButton.sizeDelta, Vector2.zero);
            Rule(photoButton, new Vector2(0, -54), new Vector2(300, 50), Ink);
            homePhotoTitle = AddText(photoButton, "Choose surroundings", new Vector2(260, 44), new Vector2(-3, -54), 23, Panel);
            AddText(photoButton, ">", new Vector2(26, 40), new Vector2(128, -54), 26, Panel);
            items.Add(new MenuItem(photoSurface, homePhotoTitle, () => ShowBackgrounds(true), null));
            AddText(home, "03 / PHYSICAL CONTROL", new Vector2(300, 24), new Vector2(282, -3), 17, Accent);
            var dialRect = CreateRect("Potentiometer dial", home, new Vector2(164, 164), new Vector2(209, -100));
            dial = dialRect.gameObject.AddComponent<FocusDialGraphic>(); dial.color = Accent; dial.raycastTarget = false;
            dialValue = Heading(dialRect, "0%", new Vector2(120, 56), new Vector2(0, 4), 44, TextAnchor.MiddleCenter);
            AddText(dialRect, "INTENSITY", new Vector2(110, 22), new Vector2(0, -30), 14, Muted, alignment: TextAnchor.MiddleCenter);
            dialState = AddText(home, "NO SIGNAL", new Vector2(135, 30), new Vector2(363, -60), 18, Ink);
            effectDetail = AddText(home, "Turn your knob.\nEase into focus.", new Vector2(135, 84), new Vector2(363, -112), 19, Muted);
            workspaceState = AddText(home, "Workspace / not selected", new Vector2(300, 30), new Vector2(282, -192), 18, Muted);
            if (debugWorkspace != null)
                AddItem(home, "Scan debug", new Vector2(150, 32), new Vector2(-90, -291),
                    () => Explain("Debug an AI scan", "Run the same scan with a frozen camera preview and detected boxes. Results are saved to a log on the headset.", "Start debug", debugWorkspace));
            AddItem(home, "Controls & field guide  >", new Vector2(300, 38), new Vector2(282, -229), StartWalkthrough);

            AddText(backgrounds, "02 / ENVIRONMENT COLLECTION", new Vector2(550, 28), new Vector2(-157, 211), 18, Accent);
            Heading(backgrounds, "Somewhere to settle.", new Vector2(800, 65), new Vector2(-32, 162), 48);
            galleryPhoto = Photo(backgrounds, new Vector2(330, 225), new Vector2(-267, 8));
            galleryPlaceholder = Heading(backgrounds, "YOUR ROOM,\nSOFTENED.", new Vector2(270, 160), new Vector2(-267, 8), 42, TextAnchor.MiddleCenter);
            galleryPlaceholder.gameObject.SetActive(false);
            galleryTitle = Heading(backgrounds, "", new Vector2(330, 48), new Vector2(-267, -137), 34);
            galleryCaption = AddText(backgrounds, "", new Vector2(330, 52), new Vector2(-267, -182), 19, Muted);
            for (int i = 0; i < Styles.Length; i++)
            {
                BackgroundStyle choice = Styles[i];
                AddItem(backgrounds, Names[i], new Vector2(490, 43), new Vector2(187, 104 - i * 48), () => choose(choice), choice);
            }
            AddItem(backgrounds, "< Home", new Vector2(155, 34), new Vector2(-354, -232), () => ShowBackgrounds(false));
            AddItem(backgrounds, "Image credits", new Vector2(155, 34), new Vector2(-165, -232), ShowCredits);
            AddText(backgrounds, "Point to preview / trigger to apply", new Vector2(490, 28), new Vector2(187, -242), 16, Muted, alignment: TextAnchor.MiddleRight);
            cursor = CreateRect("Controller reticle", panel, new Vector2(10, 10), Vector2.zero);
            AddImage(cursor, Accent); cursor.localRotation = Quaternion.Euler(0, 0, 45);
            cursor.gameObject.SetActive(false);
            calibration = CreateRect("Workspace guidance", camera, new Vector2(800, 164), Vector2.zero);
            calibration.localPosition = new Vector3(0, -0.25f, 1.15f); calibration.localScale = Vector3.one * 0.00095f;
            SetupCanvas(calibration, camera.GetComponent<Camera>(), 5); AddImage(calibration, Panel);
            Rule(calibration, new Vector2(-393, 0), new Vector2(5, 164), Accent);
            calibrationTitle = Heading(calibration, "", new Vector2(738, 52), new Vector2(0, 40), 34);
            calibrationHint = AddText(calibration, "", new Vector2(738, 84), new Vector2(0, -30), 22, Ink);
            calibration.gameObject.SetActive(false);
            motion = panel.gameObject.AddComponent<FocusUiMotion>(); motion.Tick = Animate;
            BuildInstructions(); BuildScanDebug(); ShowBackgrounds(false); RefreshPhotos(previewStyle);
            if (PlayerPrefs.GetInt("LockedIn.Walkthrough.v2", 0) == 0) StartWalkthrough();
        }

        public void SetFocusState(float amount, string mode, string state, bool bypass, string detail, bool ready)
        {
            targetAmount = Mathf.Clamp01(amount);
            bool live = state.StartsWith("LIVE", StringComparison.OrdinalIgnoreCase);
            string shortState = bypass ? "BYPASS" : live ? "LIVE" : state == "CONTROLLER MODE" ? "CONTROLLER" : "NO SIGNAL";
            dialState.text = shortState; dialState.color = live && !bypass ? new Color32(56, 111, 73, 255) : Ink;
            effectDetail.text = !string.IsNullOrWhiteSpace(detail) ? detail.Trim() : live ? "Turn your knob.\nEase into focus." : "Keep Nano powered.\nWi-Fi connects directly.";
            effectDetail.resizeTextForBestFit = true; effectDetail.resizeTextMinSize = 16; effectDetail.resizeTextMaxSize = 19;
            workspaceState.text = ready ? "Workspace / ready to focus" : "Workspace / not selected";
        }
        public void ShowCalibration(string title, string hint) { calibrationTitle.text = title; calibrationHint.text = hint; calibrationActive = true; SetCalibrationVisible(true); }
        public void HideCalibration() { calibrationActive = false; SetCalibrationVisible(false); }
        public void SetCalibrationVisible(bool visible) => calibration.gameObject.SetActive(visible && calibrationActive && !IsOpen);
        public void Toggle(BackgroundStyle selected)
        {
            if (IsOpen) { Hide(); return; }
            currentStyle = selected; ShowBackgrounds(false); panel.gameObject.SetActive(true); HighlightItem(null, selected);
        }
        public void Hide() { cursor.gameObject.SetActive(false); panel.gameObject.SetActive(false); lastHovered = null; }
        public void UpdatePointer(bool hasRay, Ray ray, bool pressed, BackgroundStyle selected)
            => UpdatePointerOnPanel(panel, cursor, hasRay, ray, pressed, selected);

        private bool UpdatePointerOnPanel(RectTransform targetPanel, RectTransform pointer,
            bool hasRay, Ray ray, bool pressed, BackgroundStyle selected)
        {
            MenuItem hovered = null;
            bool showCursor = false;
            var plane = new Plane(targetPanel.forward, targetPanel.position);
            if (hasRay && plane.Raycast(ray, out float distance) && distance > 0)
            {
                Vector3 hit = ray.GetPoint(distance); Vector2 local = targetPanel.InverseTransformPoint(hit);
                showCursor = targetPanel.rect.Contains(local);
                if (showCursor)
                {
                    pointer.anchoredPosition = local;
                    foreach (MenuItem item in items)
                    {
                        RectTransform rect = item.Surface.rectTransform;
                        if (rect.IsChildOf(targetPanel) && rect.gameObject.activeInHierarchy && rect.rect.Contains((Vector2)rect.InverseTransformPoint(hit))) { hovered = item; break; }
                    }
                }
            }
            pointer.gameObject.SetActive(showCursor); HighlightItem(hovered, selected);
            if (hovered != lastHovered)
            {
                if (hovered != null) Haptic(0.06f, 0.018f);
                if (hovered?.Style != null) RefreshPhotos(hovered.Style.Value);
                lastHovered = hovered;
            }
            if (pressed && hovered != null) { pressPulse = 1; Haptic(0.22f, 0.035f); hovered.Activate(); }
            return showCursor;
        }
        public void Dispose()
        {
            motion.Tick = null;
            DestroyUi(panel.gameObject); DestroyUi(calibration.gameObject);
            if (scanDebugPanel != null) DestroyUi(scanDebugPanel.gameObject);
            if (borderTools != null) DestroyUi(borderTools.gameObject);
            foreach (Texture2D texture in previews.Values) if (texture != null) Resources.UnloadAsset(texture);
            previews.Clear();
        }
        private static void DestroyUi(GameObject target)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
        private void Animate(float delta)
        {
            shownAmount = Mathf.Lerp(shownAmount, targetAmount, 1 - Mathf.Exp(-delta * 14));
            dial.Amount = shownAmount;
            int percent = Mathf.RoundToInt(shownAmount * 100);
            if (percent != displayedPercent) { dialValue.text = percent + "%"; displayedPercent = percent; }
            if (!IsOpen && (borderTools == null || !borderTools.gameObject.activeSelf)) return;
            pageFade.alpha = Mathf.MoveTowards(pageFade.alpha, 1, delta * 7);
            pressPulse = Mathf.MoveTowards(pressPulse, 0, delta * 8);
            cursor.localScale = Vector3.one * (1 + pressPulse * 0.65f);
            foreach (MenuItem item in items)
            {
                if (!item.Surface.gameObject.activeInHierarchy) continue;
                item.Hover = Mathf.MoveTowards(item.Hover, item.Hovered ? 1 : 0, delta * 9);
                Color resting = item.Style == currentStyle ? Selected : item.Primary ? Ink : Card;
                item.Surface.color = Color.Lerp(resting, item.Primary ? Accent : Highlight, item.Hover);
                if (item.Marker != null) item.Marker.color = item.Style == currentStyle || item.Hovered ? Accent : Line;
            }
        }
        private static void Haptic(float strength, float duration)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid && device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0, strength, duration);
        }
        private void ShowBackgrounds(bool show)
        {
            if (instructions != null) instructions.gameObject.SetActive(false);
            home.gameObject.SetActive(!show); backgrounds.gameObject.SetActive(show);
            pageFade.alpha = 0; lastHovered = null;
            RefreshPhotos(Array.IndexOf(Styles, currentStyle) >= 0 ? currentStyle : previewStyle);
        }
        private void HighlightItem(MenuItem hovered, BackgroundStyle selected)
        {
            if (currentStyle != selected) { currentStyle = selected; RefreshPhotos(selected); }
            foreach (MenuItem item in items) item.Hovered = item == hovered;
        }
        private void RefreshPhotos(BackgroundStyle style)
        {
            int index = Array.IndexOf(Styles, style); if (index < 0) return;
            previewStyle = style;
            if (!previews.TryGetValue(style, out Texture2D texture))
            {
                texture = ResourcesNames[index].Length == 0 ? null : Resources.Load<Texture2D>("LockedIn/Interface/Previews/" + ResourcesNames[index]);
                previews[style] = texture;
            }
            galleryPhoto.texture = texture; galleryPhoto.color = texture == null ? Selected : Color.white;
            galleryPlaceholder.gameObject.SetActive(texture == null);
            galleryPlaceholder.text = style == BackgroundStyle.RoomBlur ? "YOUR ROOM,\nSOFTENED." : "PREVIEW\nUNAVAILABLE";
            galleryTitle.text = Names[index]; galleryCaption.text = Captions[index];
            int chosen = Array.IndexOf(Styles, currentStyle);
            if (chosen >= 0)
            {
                homePhotoTitle.text = Names[chosen];
                if (!previews.TryGetValue(currentStyle, out Texture2D selectedTexture))
                {
                    selectedTexture = ResourcesNames[chosen].Length == 0 ? null : Resources.Load<Texture2D>("LockedIn/Interface/Previews/" + ResourcesNames[chosen]);
                    previews[currentStyle] = selectedTexture;
                }
                homePhoto.texture = selectedTexture; homePhoto.color = selectedTexture == null ? Selected : Color.white;
            }
            else { homePhoto.texture = texture; homePhoto.color = texture == null ? Selected : Color.white; }
        }
        private void AddAction(RectTransform parent, string number, string title, string subtitle, Vector2 position, Action activate, bool primary = false)
        {
            var rect = CreateRect(title, parent, new Vector2(490, 68), position);
            var surface = AddImage(rect, primary ? Ink : Card);
            AddText(rect, number, new Vector2(38, 30), new Vector2(-210, 0), 19, primary ? Panel : Muted);
            var label = AddText(rect, title, new Vector2(370, 32), new Vector2(8, 13), 26, primary ? Panel : Ink);
            AddText(rect, subtitle, new Vector2(370, 25), new Vector2(8, -17), 18, primary ? new Color32(199, 211, 196, 255) : Muted);
            AddText(rect, ">", new Vector2(24, 35), new Vector2(219, 0), 28, primary ? Panel : Accent);
            items.Add(new MenuItem(surface, label, activate, null) { Primary = primary });
        }
        private void AddItem(RectTransform parent, string title, Vector2 size, Vector2 position, Action activate, BackgroundStyle? style = null)
        {
            var rect = CreateRect(title, parent, size, position); var surface = AddImage(rect, Card);
            Text label = AddText(rect, title, size - new Vector2(48, 0), new Vector2(4, 0), size.y < 40 ? 18 : 23, Ink);
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 16; label.resizeTextMaxSize = label.fontSize;
            var item = new MenuItem(surface, label, activate, style);
            if (style.HasValue) item.Marker = AddImage(CreateRect("Selected environment", rect, new Vector2(4, size.y - 16), new Vector2(-size.x / 2 + 9, 0)), Line);
            items.Add(item);
        }
        private static void SetupCanvas(RectTransform rect, Camera camera, int order)
        {
            var canvas = rect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera; canvas.sortingOrder = order;
        }
        private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static void Rule(Transform parent, Vector2 position, Vector2 size, Color color) => AddImage(CreateRect("Rule", parent, size, position), color);
        private static void Mark(Transform parent, Vector2 position, float size)
        {
            Rule(parent, position + new Vector2(-size * .35f, 0), new Vector2(size * .18f, size), Accent);
            Rule(parent, position + new Vector2(0, -size * .41f), new Vector2(size * .7f, size * .18f), Accent);
            Rule(parent, position + new Vector2(size * .35f, size * .18f), new Vector2(size * .18f, size * .64f), Accent);
        }
        private static RawImage Photo(Transform parent, Vector2 size, Vector2 position)
        {
            var photo = CreateRect("Environment photograph", parent, size, position).gameObject.AddComponent<RawImage>();
            photo.raycastTarget = false; photo.color = Selected;
            // Thumbnails are 2:1. Center crop without stretching the panorama.
            float fraction = size.x / size.y / 2;
            photo.uvRect = new Rect((1 - fraction) / 2, 0, fraction, 1); return photo;
        }
        private Text Heading(RectTransform parent, string value, Vector2 size, Vector2 position, int fontSize, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = AddText(parent, value, size, position, fontSize, Ink, alignment: alignment);
            label.font = headingFont; label.verticalOverflow = VerticalWrapMode.Overflow; return label;
        }
        private Text AddText(RectTransform parent, string value, Vector2 size, Vector2 position, int fontSize, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = CreateRect(value.Length > 50 ? "Text" : value, parent, size, position).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = fontSize; label.fontStyle = style;
            label.alignment = alignment; label.color = color; label.raycastTarget = false; label.supportRichText = false; return label;
        }
        private sealed class MenuItem
        {
            public readonly Image Surface; public readonly Text Label; public readonly Action Activate;
            public readonly BackgroundStyle? Style; public Image Marker; public bool Hovered, Primary; public float Hover;
            public MenuItem(Image surface, Text label, Action activate, BackgroundStyle? style)
            { Surface = surface; Label = label; Activate = activate; Style = style; }
        }

#if UNITY_EDITOR
        public static string ValidateEditorInteractions(Camera camera)
        {
            int detected = 0, depth = 0, manual = 0, chosen = 0;
            var menu = new FocusHomeMenu(camera.transform, _ => chosen++, () => manual++, () => depth++, () => detected++);
            try
            {
                void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
                void Click(float x, float y)
                {
                    Vector3 point = menu.panel.TransformPoint(new Vector3(x, y, 0));
                    menu.UpdatePointer(true, new Ray(camera.transform.position, point - camera.transform.position), true, BackgroundStyle.SecludedBeach);
                }
                menu.ShowBackgrounds(false);
                Click(-187, -57); Check(menu.instructions.gameObject.activeSelf, "AI explanation did not open");
                Click(307, -218); Check(detected == 1, "AI callback did not fire exactly once");
                menu.ShowBackgrounds(false); Click(-187, -135); Click(307, -218); Check(depth == 1, "Depth callback failed");
                menu.ShowBackgrounds(false); Click(-187, -213); Click(307, -218); Check(manual == 1, "Manual callback failed");
                menu.ShowBackgrounds(false); Click(282, 104); Check(menu.backgrounds.gameObject.activeSelf, "Photo tile did not open collection");
                for (int i = 0; i < Styles.Length; i++) Click(187, 104 - i * 48);
                Check(chosen == 7, "Not all seven environments were selectable");
                Click(-165, -232); Check(menu.instructionTitle.text == "The view, credited.", "Image credits inaccessible");
                Click(307, -218); Check(menu.backgrounds.gameObject.activeSelf, "Credits return failed");
                menu.ShowBackgrounds(false); Click(282, -229); Check(menu.walkthroughPage == 0 && menu.instructions.gameObject.activeSelf, "Guide inaccessible");
                for (int i = 0; i < 4; i++) Click(307, -218);
                Check(menu.walkthroughPage == 4, "Five guide slides are not reachable");
                menu.ShowCalibration("Test", "Test"); Check(!menu.calibration.gameObject.activeSelf, "Calibration overlapped Home");
                menu.Hide(); menu.SetCalibrationVisible(true); menu.Animate(0.02f);
                Check(menu.calibration.gameObject.activeSelf && !menu.panel.gameObject.activeSelf, "Calibration visibility failed");
                foreach (Transform child in camera.transform)
                    Check(child.name != "Focus instrument / status", "Floating status bar still exists");
                menu.HideCalibration(); menu.Toggle(BackgroundStyle.SecludedBeach);
                Check(menu.IsOpen && menu.home.gameObject.activeSelf, "Grip reopening did not reset Home");
                int total = detected + depth + manual + chosen;
                Click(900, 900); Check(total == detected + depth + manual + chosen, "Outside ray activated a button");
                return "PASS: controller ray routing, three detector callbacks, seven environments, credits return, five guide slides, calibration visibility, no floating status bar, grip reopen, outside-ray rejection.";
            }
            finally { menu.Dispose(); }
        }

        // Editor-only presentation fixture; never used by the headset build or saved to a scene.
        public static IDisposable CreateEditorPreview(Camera camera, string page)
        {
            var menu = new FocusHomeMenu(camera.transform, _ => { }, () => { }, () => { }, () => { });
            menu.currentStyle = BackgroundStyle.SecludedBeach;
            menu.ShowBackgrounds(page == "collection");
            if (page == "guide") { menu.walkthroughPage = 3; menu.ShowWalkthrough(); }
            menu.SetFocusState(0.64f, "Environment", "LIVE", false, "", true);
            for (int i = 0; i < 60; i++) menu.Animate(1 / 60f);
            return menu;
        }
#endif
    }
}
