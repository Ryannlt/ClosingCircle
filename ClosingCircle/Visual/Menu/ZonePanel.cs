using ClosingCircle.Core;
using ClosingCircle.Systems;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The panel. Two audiences from one window: any player adjusting how the zone looks on their own screen, and an
// admin driving the mod without typing rc.
//
// Parented into the game's Main Canvas like the HUD, so it inherits the scaler and the player's UI scale
// setting. Unlike the HUD it is last sibling and carries a GraphicRaycaster, because it is clicked.

namespace ClosingCircle.Visual.Menu
{
    public static class ZonePanel
    {
        private const string ObjectName = "ClosingCirclePanel";
        private const string CanvasName = "Main Canvas";

        private const KeyCode OpenKey = KeyCode.F3;

        // Above anything the game draws. Unity's ceiling is 32767, and the escape menu is the thing this has
        // to beat.
        private const int SortingOrder = 30000;

        private static readonly Vector2 Reference = new Vector2(2560f, 1440f);
        private static readonly Vector2 WindowSize = new Vector2(1240f, 980f);
        private const float RailWidth = 224f;

        private static GameObject _object;
        private static GameObject _window;
        private static CanvasScaler _scaler;
        private static Transform _railHost;
        private static Transform _paneHost;
        private static Transform _overlayHost;
        private static ScrollRect _scroll;
        private static TextMeshProUGUI _wire;

        private static readonly List<MenuCategory> Categories = new List<MenuCategory>();
        private static string _current;
        private static bool _open;

        private static CursorLockMode _wasLock;
        private static bool _wasVisible;

        public static bool IsOpen => _open;

        // Popups live here rather than in the scroll view, whose RectMask2D would clip them.
        public static Transform Overlay => _overlayHost;

        public static void CloseOverlay()
        {
            if (_overlayHost == null) return;

            for (int i = _overlayHost.childCount - 1; i >= 0; i--)
                Object.Destroy(_overlayHost.GetChild(i).gameObject);
        }

        public static bool OverlayOpen => _overlayHost != null && _overlayHost.childCount > 0;

        public static void Reset()
        {
            Close();

            // Unconditional rather than relying on Close: a panel open across a map change is torn down by the
            // assembly reload, not by us, so nothing would have released the flag.
            InputFocus.Reset();

            if (_object != null) Object.Destroy(_object);

            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);

            _object = null;
            _scaler = null;
            _scroll = null;
            _overlayHost = null;
            Categories.Clear();

            // Edits staged against the previous round's settings mean nothing now.
            MenuCategories.ClearPending();
        }

        // Driven from the client's frame callback, like everything else here.
        public static void Step()
        {
            if (Input.GetKeyDown(OpenKey)) Toggle();

            if (!_open) return;

            // Escape closes an open dropdown and nothing more. It deliberately does not close the panel: it
            // is the key that opens the game's own menu, and the panel is meant to stay up over it. F3 and
            // the X are the ways out.
            if (Input.GetKeyDown(KeyCode.Escape) && OverlayOpen)
            {
                CloseOverlay();
                return;
            }

            // Re-asserted every frame because the game sets the cursor whenever its own state changes.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void Toggle()
        {
            if (_open) Close();
            else Open();
        }

        public static void Open()
        {
            EnsureBuilt();
            if (_object == null) return;

            _open = true;
            _window.SetActive(true);

            _wasLock = Cursor.lockState;
            _wasVisible = Cursor.visible;

            // The game's own "a UI has focus" flag, which stops the character reading the keyboard while
            // somebody types coordinates into a field.
            InputFocus.Take();

            MatchGameScale();

            // Asking once on open, so the tools are already unlocked by the time somebody looks for them.
            AdminAccess.ProbeOnce();

            ClientDisplay.NotePanelOpened();
            Refresh();
        }

        public static void Close()
        {
            if (!_open) return;

            _open = false;
            InputFocus.Release();
            CloseOverlay();

            if (_window != null) _window.SetActive(false);

            Cursor.lockState = _wasLock;
            Cursor.visible = _wasVisible;
        }

        public static void Sends(string command)
        {
            if (_wire == null) return;

            _wire.text = string.IsNullOrEmpty(command) ? BuildInfo.Line : command;

            _wire.color = string.IsNullOrEmpty(command) ? MenuWidgets.Faint : MenuWidgets.Ok;
        }

        // Rebuilds the visible category, which is how a category reflects state that changed elsewhere: an
        // admin unlocking, or a push arriving.
        public static void Refresh()
        {
            BuildRail();
            ShowCategory(_current);
        }

        private static void ShowCategory(string name)
        {
            MenuCategory chosen = null;

            for (int i = 0; i < Categories.Count; i++)
            {
                if (Categories[i].Name != name) continue;

                chosen = Categories[i];
                break;
            }

            if (chosen == null && Categories.Count > 0) chosen = Categories[0];
            if (chosen == null) return;

            _current = chosen.Name;

            for (int i = _paneHost.childCount - 1; i >= 0; i--) Object.Destroy(_paneHost.GetChild(i).gameObject);

            // Destroy is deferred to the end of the frame, so the fresh content would otherwise be laid out
            // alongside the old children it is replacing.
            for (int i = 0; i < _paneHost.childCount; i++) _paneHost.GetChild(i).gameObject.SetActive(false);

            GameObject pane = MenuWidgets.Node("Pane", _paneHost);
            MenuWidgets.Stretch(pane.GetComponent<RectTransform>(), 0f);
            MenuWidgets.Column(pane, 10f, new RectOffset(26, 26, 22, 22));

            if (chosen.AdminOnly && !AdminAccess.IsAdmin) MenuCategories.BuildLocked(pane.transform);
            else chosen.Build(pane.transform);

            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;

            BuildRail();
        }

        private static void BuildRail()
        {
            if (_railHost == null) return;

            for (int i = _railHost.childCount - 1; i >= 0; i--)
                Object.Destroy(_railHost.GetChild(i).gameObject);

            for (int i = 0; i < Categories.Count; i++)
            {
                MenuCategory category = Categories[i];
                bool locked = category.AdminOnly && !AdminAccess.IsAdmin;
                string name = category.Name;

                GameObject tab = MenuWidgets.Node("Tab", _railHost);
                MenuWidgets.Size(tab, 0f, 46f);
                Image background = MenuWidgets.Panel(tab, name == _current
                    ? new Color(MenuWidgets.Accent.r, MenuWidgets.Accent.g, MenuWidgets.Accent.b, 0.07f)
                    : Color.clear);

                background.raycastTarget = true;
                tab.AddComponent<Button>().onClick.AddListener(() => ShowCategory(name));

                Color ink = locked
                    ? MenuWidgets.Faint
                    : name == _current ? MenuWidgets.Ink : MenuWidgets.Dim;

                TextMeshProUGUI label = MenuWidgets.Text(tab.transform, name.ToUpperInvariant(),
                                                         MenuWidgets.Body, ink);

                MenuWidgets.Stretch(label.rectTransform, 0f);
                label.rectTransform.offsetMin = new Vector2(22f, 0f);

                if (!locked) continue;

                // A word rather than a glyph: the font is borrowed off a live Holdfast label at runtime and
                // there is no promise it carries a padlock.
                TextMeshProUGUI tag = MenuWidgets.Text(tab.transform, "LOCKED", MenuWidgets.Tiny,
                                                       MenuWidgets.Faint, TextAlignmentOptions.Right);

                MenuWidgets.Stretch(tag.rectTransform, 0f);
                tag.rectTransform.offsetMax = new Vector2(-18f, 0f);
            }
        }

        private static void EnsureBuilt()
        {
            if (_object != null) return;

            _object = MenuWidgets.Node("Root", null);
            _object.name = ObjectName;

            Attach();

            _window = MenuWidgets.Node("Window", _object.transform);
            MenuWidgets.Panel(_window, MenuWidgets.Window);

            RectTransform windowRect = _window.GetComponent<RectTransform>();
            windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = WindowSize;

            BuildChrome();
            RegisterCategories();

            _current = Categories[0].Name;
            ShowCategory(_current);
        }

        private static void BuildChrome()
        {
            GameObject title = MenuWidgets.Node("Title", _window.transform);
            MenuWidgets.Panel(title, MenuWidgets.Rail);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 54f);
            titleRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI heading = MenuWidgets.Text(title.transform, "CLOSING CIRCLE", MenuWidgets.Body,
                                                       MenuWidgets.Ink);
            MenuWidgets.Stretch(heading.rectTransform, 0f);
            heading.rectTransform.offsetMin = new Vector2(22f, 0f);
            heading.characterSpacing = 12f;

            GameObject close = MenuWidgets.Node("Close", title.transform);
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(54f, 54f);
            closeRect.anchoredPosition = new Vector2(-6f, 0f);

            MenuWidgets.Panel(close, Color.clear).raycastTarget = true;
            close.AddComponent<Button>().onClick.AddListener(Close);

            TextMeshProUGUI cross = MenuWidgets.Text(close.transform, "X", MenuWidgets.Body, MenuWidgets.Faint,
                                                     TextAlignmentOptions.Center);
            MenuWidgets.Stretch(cross.rectTransform, 0f);

            // Rail
            GameObject rail = MenuWidgets.Node("Rail", _window.transform);
            MenuWidgets.Panel(rail, MenuWidgets.Rail);
            RectTransform railRect = rail.GetComponent<RectTransform>();
            railRect.anchorMin = new Vector2(0f, 0f);
            railRect.anchorMax = new Vector2(0f, 1f);
            railRect.pivot = new Vector2(0f, 1f);
            railRect.sizeDelta = new Vector2(RailWidth, -(54f + 48f));
            railRect.anchoredPosition = new Vector2(0f, -54f);

            MenuWidgets.Column(rail, 2f, new RectOffset(0, 0, 12, 12));
            _railHost = rail.transform;

            // Content, scrollable: Display is taller than the window and a stage list grows without bound.
            GameObject viewport = MenuWidgets.Node("Viewport", _window.transform);
            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(RailWidth, 48f);
            viewRect.offsetMax = new Vector2(0f, -54f);

            // Clipped, and a raycast target so the wheel reaches the scroll handler under the pointer.
            viewport.AddComponent<RectMask2D>();
            MenuWidgets.Panel(viewport, Color.clear).raycastTarget = true;

            GameObject content = MenuWidgets.Node("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            // A fresh RectTransform carries Unity's default 100x100. Stretched between the viewport's edges on
            // a centred pivot, that leftover 100 hangs 50px off each side and clips every label in the pane.
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);

            MenuWidgets.Column(content, 0f, new RectOffset(0, 0, 0, 0));

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _scroll = viewport.AddComponent<ScrollRect>();
            _scroll.content = contentRect;
            _scroll.viewport = viewRect;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
            _scroll.inertia = false;

            _paneHost = content.transform;

            // The strip that shows what the panel just sent, so an admin can see the command behind a click.
            GameObject wire = MenuWidgets.Node("Wire", _window.transform);
            MenuWidgets.Panel(wire, MenuWidgets.Rail);
            RectTransform wireRect = wire.GetComponent<RectTransform>();
            wireRect.anchorMin = Vector2.zero;
            wireRect.anchorMax = new Vector2(1f, 0f);
            wireRect.pivot = new Vector2(0.5f, 0f);
            wireRect.sizeDelta = new Vector2(0f, 48f);

            GameObject overlay = MenuWidgets.Node("Overlay", _window.transform);
            MenuWidgets.Stretch(overlay.GetComponent<RectTransform>(), 0f);
            overlay.transform.SetAsLastSibling();
            _overlayHost = overlay.transform;

            _wire = MenuWidgets.Text(wire.transform, BuildInfo.Line, MenuWidgets.Tiny, MenuWidgets.Faint);
            MenuWidgets.Stretch(_wire.rectTransform, 0f);
            _wire.rectTransform.offsetMin = new Vector2(22f, 0f);
        }

        private static void RegisterCategories()
        {
            Categories.Clear();
            Categories.Add(new MenuCategory("Display", false, MenuCategories.BuildDisplay));
            Categories.Add(new MenuCategory("Zone", true, MenuCategories.BuildZone));
            Categories.Add(new MenuCategory("Stages", true, MenuCategories.BuildStages));
            Categories.Add(new MenuCategory("Actions", true, MenuCategories.BuildActions));
        }

        // Its own root canvas, unlike the HUD, which is parented into the game's. A nested canvas can only
        // sort within its own root, so it could never draw over the escape menu however high its order was.
        // The escape menu is the one place a player can type without driving their character, which is
        // exactly where this panel needs to be usable.
        private static void Attach()
        {
            var own = _object.AddComponent<Canvas>();
            own.renderMode = RenderMode.ScreenSpaceOverlay;
            own.sortingOrder = SortingOrder;

            _scaler = _object.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = Reference;
            _scaler.matchWidthOrHeight = 1f;

            MatchGameScale();
            MenuWidgets.Stretch(_object.GetComponent<RectTransform>(), 0f);

            // Releases the input flag if the scene is torn down under us, which a disconnect does and no
            // callback announces.
            _object.AddComponent<InputFocusGuard>();

            // Unlike the HUD this one is clicked, so it needs a raycaster and an event system to feed it.
            _object.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
        }

        // Standing on its own canvas costs the panel the UI scale it used to inherit, so the setting is read
        // off the game's own scaler instead of guessed at. Re-read on every open, since it can change while
        // the panel is closed.
        private static void MatchGameScale()
        {
            if (_scaler == null) return;

            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas == null) return;

            var theirs = canvas.GetComponent<CanvasScaler>();
            if (theirs == null || theirs.referenceResolution.x <= 0f) return;

            _scaler.referenceResolution = theirs.referenceResolution;
            _scaler.matchWidthOrHeight = theirs.matchWidthOrHeight;
        }

        // The game will normally have one. Adding a second would break both, so this only fills a gap.
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject events = MenuWidgets.Node("Events", null);
            events.AddComponent<EventSystem>();
            events.AddComponent<StandaloneInputModule>();
        }
    }

    public class MenuCategory
    {
        public readonly string Name;
        public readonly bool AdminOnly;
        public readonly System.Action<Transform> Build;

        public MenuCategory(string name, bool adminOnly, System.Action<Transform> build)
        {
            Name = name;
            AdminOnly = adminOnly;
            Build = build;
        }
    }
}
