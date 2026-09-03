using ClosingCircle.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Read-only status for the local player: how big the zone is now, and how long until it next moves. The
// announcer only speaks once per stage and the wall cannot be seen from behind, so without this neither is
// answerable at a glance.
//
// Two pills side by side, matching the game's own reinforcement and base-spawn row. They are parented into the
// game's Main Canvas and follow the Top Info Bar's visibility, so they inherit its scaling and the player's UI
// scale setting, and they stay off screens like the spawn menu where that bar is hidden.

namespace ClosingCircle.Visual
{
    public static class ZoneHud
    {
        private const string ObjectName = "ClosingCircleHud";
        private const string CanvasName = "Main Canvas";
        private const string TopBarName = "Top Info Bar";

        // Below the game's row, using the same gap it leaves under the score bar.
        private const float TopOffset = 128f;
        private const float Gap = 11f;

        private const float FontSize = 19f;
        private const float PadX = 17f;
        private const float PadY = 7f;

        // Only reached when the game's canvas cannot be found. Zero so the game's own UI still wins.
        private const int FallbackSortingOrder = 0;

        // The game's own UI reference, so every size above is in the units its HUD is authored in.
        private static readonly Vector2 Reference = new Vector2(2560f, 1440f);
        private static readonly Color Panel = new Color(0f, 0f, 0f, 0.78f);

        private static GameObject _object;
        private static Pill _size;
        private static Pill _stage;

        private static Transform _topBar;
        private static bool _attached;

        private static TMP_FontAsset _font;
        private static float _nextSearchAt;

        private class Pill
        {
            public GameObject Object;
            public RectTransform Rect;
            public TextMeshProUGUI Label;

            public bool Visible => Object.activeSelf;
            public float Width => Object.activeSelf ? Rect.sizeDelta.x : 0f;

            public void Apply(string text)
            {
                if (text == null)
                {
                    if (Object.activeSelf) Object.SetActive(false);
                    return;
                }

                if (!Object.activeSelf) Object.SetActive(true);
                if (Label.text != text) Label.text = text;

                Vector2 size = Label.GetPreferredValues(text);
                Rect.sizeDelta = new Vector2(size.x + PadX * 2f, size.y + PadY * 2f);
            }

            public void Hide()
            {
                if (Object.activeSelf) Object.SetActive(false);
            }

            public void Place(float x) => Rect.anchoredPosition = new Vector2(x, -TopOffset);
        }

        public static void Reset()
        {
            Hide();

            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);

            _topBar = null;
            _attached = false;
            _font = null;
            _nextSearchAt = 0f;
        }

        public static void Hide()
        {
            if (_object != null) Object.Destroy(_object);

            _object = null;
            _size = null;
            _stage = null;
            _topBar = null;
            _attached = false;
        }

        public static void Update(ZoneSnapshot zone, float timeRemaining)
        {
            if (!ZoneService.Hud)
            {
                Hide();
                return;
            }

            EnsureBuilt();
            SearchForGameUi();

            // The bar is switched off on the spawn and faction screens, and the zone status has no business
            // sitting over them.
            if (_topBar != null && !_topBar.gameObject.activeInHierarchy)
            {
                _size.Hide();
                _stage.Hide();
                return;
            }

            _size.Apply($"CIRCLE RADIUS {zone.Radius:0}M");
            _stage.Apply(DescribeSchedule(timeRemaining));

            // Centred as a pair, so the radius pill sits in the middle on its own once the last stage is done.
            float gap = _stage.Visible ? Gap : 0f;
            float total = _size.Width + gap + _stage.Width;

            _size.Place(-total * 0.5f + _size.Width * 0.5f);
            if (_stage.Visible) _stage.Place(total * 0.5f - _stage.Width * 0.5f);
        }

        // Split, because the two states want different things said: mid close the useful number is where it is
        // heading, and between stages it is how long you have left.
        private static string DescribeSchedule(float timeRemaining)
        {
            ZonePlan plan = ZoneService.Plan;

            int active = PlanEvaluator.ActiveStageIndex(plan, timeRemaining);
            if (active >= 0)
            {
                Stage closing = plan.Stages[active];
                return $"CLOSING TO {closing.Radius:0}M IN {Clock(timeRemaining - closing.ToTime)}";
            }

            // Stages are held in order, so the first one still ahead of the clock is the next to run.
            for (int i = 0; i < plan.Count; i++)
            {
                Stage stage = plan.Stages[i];
                if (timeRemaining < stage.FromTime) continue;

                return $"NEXT PHASE IN {Clock(timeRemaining - stage.FromTime)}";
            }

            return null;
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));

            return total >= 60 ? $"{total / 60}:{total % 60:00}" : $"{total}S";
        }

        // The font is lifted off a live Holdfast label and the pills are moved into the game's own canvas,
        // because a mod ships no assets and cannot reach the game's assembly to ask for either. Both are
        // retried on a timer rather than every frame, since the searches are not cheap and the game's UI may
        // not be up when the mod first ticks.
        private static void SearchForGameUi()
        {
            if ((_attached && _font != null) || Time.time < _nextSearchAt) return;

            _nextSearchAt = Time.time + 1f;

            if (!_attached) Attach();
            if (_font == null) AdoptGameFont();
        }

        private static void Attach()
        {
            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas == null) return;

            Transform bar = canvas.transform.Find(TopBarName);

            _object.transform.SetParent(canvas.transform, false);

            // First sibling, so every other element in the game's canvas draws over this one.
            _object.transform.SetAsFirstSibling();

            // Our own canvas would only fight the one we just joined, and its scaler would ignore the player's
            // UI scale setting where the game's follows it.
            Canvas own = _object.GetComponent<Canvas>();
            if (own != null)
            {
                Object.Destroy(_object.GetComponent<CanvasScaler>());
                Object.Destroy(own);
            }

            Stretch(_object.GetComponent<RectTransform>());

            _topBar = bar;
            _attached = true;

            Logger.Log(bar == null
                    ? $"HUD joined '{CanvasName}' but found no '{TopBarName}' to follow."
                    : $"HUD joined '{CanvasName}' and follows '{TopBarName}'.",
                LogLevel.DEBUG);
        }

        private static void AdoptGameFont()
        {
            TextMeshProUGUI[] labels = Object.FindObjectsOfType<TextMeshProUGUI>();

            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null || labels[i].font == null) continue;
                if (labels[i].gameObject == _size.Object || labels[i].gameObject == _stage.Object) continue;

                _font = labels[i].font;
                _size.Label.font = _font;
                _stage.Label.font = _font;

                Logger.Log($"HUD adopted the game font '{_font.name}'.", LogLevel.DEBUG);
                return;
            }
        }

        private static void EnsureBuilt()
        {
            if (_object != null) return;

            // Belongs to the round, so deliberately not DontDestroyOnLoad: surviving a scene change is how a
            // previous round's object ends up orphaned with every static that referenced it already cleared.
            _object = new GameObject(ObjectName);
            _attached = false;
            _topBar = null;

            // Zeroed so a HUD switched back on mid round joins the game's canvas this frame, not a second later.
            _nextSearchAt = 0f;

            // Stands alone until the game's canvas turns up, so the HUD still works if the names ever change.
            var canvas = _object.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = FallbackSortingOrder;

            var scaler = _object.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;

            // Match height only, so an ultrawide screen does not shrink the text.
            scaler.matchWidthOrHeight = 1f;

            // No GraphicRaycaster on purpose: nothing here is clickable and it must never eat a click.
            _size = BuildPill("Size");
            _stage = BuildPill("Stage");
        }

        private static Pill BuildPill(string name)
        {
            var pill = new Pill { Object = new GameObject(name) };
            pill.Object.transform.SetParent(_object.transform, false);

            pill.Rect = pill.Object.AddComponent<RectTransform>();
            pill.Rect.anchorMin = pill.Rect.anchorMax = new Vector2(0.5f, 1f);
            pill.Rect.pivot = new Vector2(0.5f, 1f);

            // Opts out of any layout group the game's canvas puts on its children.
            pill.Object.AddComponent<LayoutElement>().ignoreLayout = true;

            var background = pill.Object.AddComponent<Image>();
            background.color = Panel;
            background.raycastTarget = false;

            var label = new GameObject("Label");
            label.transform.SetParent(pill.Object.transform, false);

            Stretch(label.AddComponent<RectTransform>());

            pill.Label = label.AddComponent<TextMeshProUGUI>();
            pill.Label.fontSize = FontSize;
            pill.Label.color = Color.white;
            pill.Label.alignment = TextAlignmentOptions.Center;
            pill.Label.enableWordWrapping = false;
            pill.Label.raycastTarget = false;

            if (_font != null) pill.Label.font = _font;

            pill.Object.SetActive(false);
            return pill;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
