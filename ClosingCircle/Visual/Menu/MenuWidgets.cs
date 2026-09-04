using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The panel's vocabulary. Every control is built here so the categories stay a list of what they contain rather
// than a wall of RectTransform arithmetic, and so one change to the look reaches all of them.
//
// Layout is done with layout groups rather than anchored positions: rows size themselves to their content, and
// nothing has to be re-measured when a label changes.

namespace ClosingCircle.Visual.Menu
{
    public static class MenuWidgets
    {
        public static readonly Color Ink = new Color(0.949f, 0.925f, 0.937f);
        public static readonly Color Dim = new Color(0.604f, 0.561f, 0.588f);
        public static readonly Color Faint = new Color(0.42f, 0.384f, 0.416f);
        public static readonly Color Accent = new Color(1f, 0.235f, 0.235f);
        public static readonly Color Ok = new Color(0.498f, 0.82f, 0.545f);

        public static readonly Color Window = new Color(0.11f, 0.09f, 0.106f, 0.97f);
        public static readonly Color Rail = new Color(0.071f, 0.055f, 0.067f, 1f);
        public static readonly Color Raised = new Color(0.149f, 0.125f, 0.153f, 1f);
        public static readonly Color Line = new Color(0.227f, 0.188f, 0.22f, 1f);

        public const float Body = 18f;
        public const float Small = 16f;
        public const float Tiny = 14f;

        public static GameObject Node(string name, Transform parent)
        {
            var node = new GameObject(GameFont.Mine + name);
            node.AddComponent<RectTransform>();
            node.transform.SetParent(parent, false);
            return node;
        }

        public static Image Panel(GameObject node, Color colour)
        {
            Image image = node.AddComponent<Image>();
            image.color = colour;
            return image;
        }

        public static VerticalLayoutGroup Column(GameObject node, float spacing, RectOffset padding)
        {
            VerticalLayoutGroup group = node.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        public static HorizontalLayoutGroup Line_(GameObject node, float spacing, RectOffset padding)
        {
            HorizontalLayoutGroup group = node.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            group.childAlignment = TextAnchor.MiddleLeft;
            return group;
        }

        // A zero leaves that axis unset, which for width means the layout group asks the components on this
        // object and gets nothing: an Image contributes no size and a TMP label is a child, not a sibling. So a
        // zero width is only ever right for something that also sets flexibleWidth.
        public static LayoutElement Size(GameObject node, float width, float height)
        {
            LayoutElement element = node.GetComponent<LayoutElement>();
            if (element == null) element = node.AddComponent<LayoutElement>();

            if (width > 0f) element.preferredWidth = element.minWidth = width;
            if (height > 0f) element.preferredHeight = element.minHeight = height;

            return element;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size, Color colour,
                                           TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject node = Node("Label", parent);

            var label = node.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = colour;
            label.alignment = alignment;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            GameFont.Apply(label);
            return label;
        }

        public static Button Press(Transform parent, string text, Action clicked, bool primary = false,
                                   float width = 170f)
        {
            GameObject node = Node("Button", parent);
            Panel(node, primary ? new Color(Accent.r, Accent.g, Accent.b, 0.16f) : Raised);

            var button = node.AddComponent<Button>();
            Size(node, width, 40f);

            TextMeshProUGUI label = Text(node.transform, text.ToUpperInvariant(), Small,
                                         primary ? Ink : Dim, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 12f);

            if (clicked != null) button.onClick.AddListener(() => clicked());
            return button;
        }

        // Kept in place rather than hidden, so a disabled Apply still shows where the control is.
        public static void Enable(Button button, bool on)
        {
            if (button == null) return;

            button.interactable = on;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = on ? Ink : Faint;

            var background = button.GetComponent<Image>();
            if (background != null)
            {
                Color colour = background.color;
                background.color = new Color(colour.r, colour.g, colour.b, on ? 1f : 0.35f);
            }
        }

        // A button that owns a bool, rather than a uGUI Toggle: the Toggle's own graphics need more wiring than
        // the behaviour is worth, and this way the tick is drawn the same way everywhere.
        public static Button Check(Transform parent, bool on, Action<bool> changed)
        {
            GameObject node = Node("Check", parent);
            Image box = Panel(node, Rail);
            Size(node, 26f, 26f);

            var button = node.AddComponent<Button>();
            TextMeshProUGUI tick = Text(node.transform, on ? "X" : string.Empty, Small, Accent,
                                        TextAlignmentOptions.Center);
            Stretch(tick.rectTransform, 0f);

            bool state = on;
            box.color = state ? new Color(Accent.r, Accent.g, Accent.b, 0.16f) : Rail;

            button.onClick.AddListener(() =>
            {
                state = !state;
                tick.text = state ? "X" : string.Empty;
                box.color = state ? new Color(Accent.r, Accent.g, Accent.b, 0.16f) : Rail;

                if (changed != null) changed(state);
            });

            return button;
        }

        public static Slider Bar(Transform parent, float min, float max, float value, Action<float> changed)
        {
            GameObject node = Node("Slider", parent);
            Size(node, 0f, 26f);

            LayoutElement flexible = node.GetComponent<LayoutElement>();
            flexible.flexibleWidth = 1f;

            var slider = node.AddComponent<Slider>();

            GameObject track = Node("Track", node.transform);
            Panel(track, Line);
            RectTransform trackRect = Middle(track, 4f);

            GameObject fill = Node("Fill", track.transform);
            Panel(fill, Accent);
            Stretch(fill.GetComponent<RectTransform>(), 0f);

            GameObject handle = Node("Handle", node.transform);
            Panel(handle, Ink);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 14f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);

            if (changed != null) slider.onValueChanged.AddListener(v => changed(v));

            trackRect.hideFlags = HideFlags.None;
            return slider;
        }

        public static TMP_InputField Field(Transform parent, string text, float width, Action<string> committed)
        {
            GameObject node = Node("Field", parent);
            Panel(node, Rail);
            Size(node, width, 34f);

            var field = node.AddComponent<TMP_InputField>();

            GameObject area = Node("TextArea", node.transform);
            RectTransform areaRect = Stretch(area.GetComponent<RectTransform>(), 8f);
            area.AddComponent<RectMask2D>();

            TextMeshProUGUI label = Text(area.transform, text, Small, Ink);
            Stretch(label.rectTransform, 0f);

            field.textViewport = areaRect;
            field.textComponent = label;
            field.text = text;

            // Committed on enter or on losing focus, rather than per keystroke: every keystroke would send an
            // rc command per character.
            if (committed != null) field.onEndEdit.AddListener(v => committed(v));

            return field;
        }

        // A real dropdown. The list cannot be a child of the button, because the content pane is inside a
        // ScrollRect whose RectMask2D would clip it, so it is built on the panel's overlay layer instead and
        // positioned from the button's world corners.
        public static Button Choose(Transform parent, string[] options, int index, float width,
                                    Action<string> changed)
        {
            GameObject node = Node("Choose", parent);
            Panel(node, Rail);
            Size(node, width, 32f);

            var button = node.AddComponent<Button>();

            int at = Mathf.Clamp(index, 0, options.Length - 1);

            TextMeshProUGUI label = Text(node.transform, options[at], Small, Ink);
            Stretch(label.rectTransform, 0f);
            label.rectTransform.offsetMin = new Vector2(10f, 0f);
            label.rectTransform.offsetMax = new Vector2(-26f, 0f);

            TextMeshProUGUI caret = Text(node.transform, "v", Tiny, Dim, TextAlignmentOptions.Right);
            Stretch(caret.rectTransform, 0f);
            caret.rectTransform.offsetMax = new Vector2(-10f, 0f);

            button.onClick.AddListener(() => Popup(node.GetComponent<RectTransform>(), options, width,
                                                   picked =>
            {
                label.text = picked;
                if (changed != null) changed(picked);
            }));

            return button;
        }

        private static void Popup(RectTransform anchor, string[] options, float width, Action<string> picked)
        {
            Transform host = ZonePanel.Overlay;
            if (host == null) return;

            // A second click on the same control closes it rather than stacking another list.
            bool wasOpen = ZonePanel.OverlayOpen;
            ZonePanel.CloseOverlay();
            if (wasOpen) return;

            // Behind the list and covering the whole panel, so a click anywhere else dismisses it.
            GameObject catcher = Node("Catcher", host);
            Stretch(catcher.GetComponent<RectTransform>(), 0f);
            Panel(catcher, Color.clear).raycastTarget = true;
            catcher.AddComponent<Button>().onClick.AddListener(ZonePanel.CloseOverlay);

            GameObject list = Node("List", host);
            Panel(list, Raised);

            RectTransform listRect = list.GetComponent<RectTransform>();
            // Anchored to the overlay's centre, because that is where ScreenPointToLocalPointInRectangle
            // measures from: a local point is relative to the rect's pivot, while anchoredPosition is relative
            // to the anchor. Anchoring at the corner while positioning from the centre put the list half a
            // window down and to the left.
            listRect.anchorMin = listRect.anchorMax = new Vector2(0.5f, 0.5f);

            float height = options.Length * 32f + 8f;
            listRect.sizeDelta = new Vector2(width, height);

            // The button's own corners, expressed in the overlay's space, so the list opens where it belongs
            // however the pane happens to be scrolled.
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);

            var parentRect = host as RectTransform;

            // The panel lives in the game's canvas and we do not get to assume its render mode: a camera-space
            // canvas needs its camera passed, and an overlay one needs a null.
            var canvas = host.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            // corners[0] is the bottom-left, so the list hangs below the button; corners[1] is the top-left,
            // which is where it goes when there is no room underneath.
            bool below = true;
            Vector2 local;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, RectTransformUtility.WorldToScreenPoint(camera, corners[0]), camera, out local);

            if (local.y - height < -parentRect.rect.height * 0.5f)
            {
                below = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, RectTransformUtility.WorldToScreenPoint(camera, corners[1]), camera, out local);
            }

            listRect.pivot = new Vector2(0f, below ? 1f : 0f);
            listRect.anchoredPosition = local;

            Column(list, 0f, new RectOffset(0, 0, 4, 4));

            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];

                GameObject row = Node("Option", list.transform);
                Size(row, 0f, 32f);
                Panel(row, Color.clear).raycastTarget = true;

                row.AddComponent<Button>().onClick.AddListener(() =>
                {
                    ZonePanel.CloseOverlay();
                    if (picked != null) picked(option);
                });

                TextMeshProUGUI text = Text(row.transform, option, Small, Ink);
                Stretch(text.rectTransform, 0f);
                text.rectTransform.offsetMin = new Vector2(10f, 0f);
            }
        }

        public static RectTransform Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static RectTransform Middle(GameObject node, float thickness)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(0f, -thickness * 0.5f);
            rect.offsetMax = new Vector2(0f, thickness * 0.5f);
            return rect;
        }
    }
}
