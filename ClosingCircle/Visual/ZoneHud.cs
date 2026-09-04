using ClosingCircle.Domain;
using ClosingCircle.Systems;
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

        // Below the game's row, using the same gap it leaves under the score bar. Only a fallback now: the
        // real offset is measured, because the row above is not always there.
        private const float TopOffset = 128f;
        private const float Gap = 11f;

        // A measurement outside this band means the bar was not laid out the way this assumes, so the fixed
        // offset is used instead of putting the pills somewhere absurd.
        private const float MinTop = 52f;
        private const float MaxTop = 220f;

        // Measuring walks the bar's graphics, so it is not worth doing every frame for something that changes
        // when the game mode does.
        private const float MeasureEvery = 0.25f;

        private const float FontSize = 19f;
        private const float PadX = 17f;
        private const float PadY = 7f;

        // Only reached when the game's canvas cannot be found. Zero so the game's own UI still wins.
        private const int FallbackSortingOrder = 0;

        // The game's own UI reference, so every size above is in the units its HUD is authored in.
        private static readonly Vector2 Reference = new Vector2(2560f, 1440f);
        private static readonly Color Panel = new Color(0f, 0f, 0f, 0.78f);

        // Dim and unpanelled, so it reads as a note rather than another pill competing with the two above it.
        private const float HintFontSize = 15f;
        private const string Hint = "PRESS F3 FOR CIRCLE SETTINGS";
        private static readonly Color HintColour = new Color(1f, 1f, 1f, 0.72f);

        private static GameObject _object;
        private static Pill _size;
        private static Pill _stage;
        private static Pill _hint;

        private static Transform _topBar;
        private static bool _attached;
        private static float _nextSearchAt;

        private static float _top = TopOffset;
        private static float _nextMeasureAt;

        private static readonly Vector3[] Corners = new Vector3[4];

        private class Pill
        {
            public GameObject Object;
            public RectTransform Rect;
            public TextMeshProUGUI Label;

            public bool Visible => Object.activeSelf;
            public float Width => Object.activeSelf ? Rect.sizeDelta.x : 0f;
            public float Height => Object.activeSelf ? Rect.sizeDelta.y : 0f;

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

            public void Place(float x, float top) => Rect.anchoredPosition = new Vector2(x, -top);
        }

        public static void Reset()
        {
            Hide();

            GameObject stray = GameObject.Find(ObjectName);
            if (stray != null) Object.Destroy(stray);

            _topBar = null;
            _attached = false;
            _nextSearchAt = 0f;
            GameFont.Reset();
        }

        public static void Hide()
        {
            if (_object != null) Object.Destroy(_object);

            _object = null;
            _size = null;
            _stage = null;
            _hint = null;
            _topBar = null;
            _attached = false;

            _top = TopOffset;
            _nextMeasureAt = 0f;
        }

        public static void Update(ZoneSnapshot zone, float timeRemaining)
        {
            // The player's own switch, which falls back to the server's when they have not set one.
            if (!ClientDisplay.Hud)
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
                _hint.Hide();
                return;
            }

            _size.Apply($"CIRCLE RADIUS {zone.Radius:0}M");
            _stage.Apply(DescribeSchedule(timeRemaining));

            // Centred as a pair, so the radius pill sits in the middle on its own once the last stage is done.
            float gap = _stage.Visible ? Gap : 0f;
            float total = _size.Width + gap + _stage.Width;

            float top = MeasuredTop();

            _size.Place(-total * 0.5f + _size.Width * 0.5f, top);
            if (_stage.Visible) _stage.Place(total * 0.5f - _stage.Width * 0.5f, top);

            // A hotkey nobody knows about is a feature nobody uses, so it is advertised until it has been
            // used once and then never again.
            _hint.Apply(ClientDisplay.SeenPanel ? null : Hint);
            if (_hint.Visible) _hint.Place(0f, top + _size.Height + Gap);
        }

        // How far down the game's own bar actually reaches. Free roam and some modes drop the reinforcement
        // row entirely, and a fixed offset then leaves an obvious hole above our pills.
        //
        // Measured from the bar's visible graphics rather than by looking for a named object, because the name
        // of that row lives in scene data the mod cannot read, and a measurement adapts to whatever the mode
        // happens to show.
        private static float MeasuredTop()
        {
            if (Time.unscaledTime < _nextMeasureAt) return _top;
            _nextMeasureAt = Time.unscaledTime + MeasureEvery;

            _top = TopOffset;

            var bar = _topBar as RectTransform;
            var host = _object == null ? null : _object.GetComponent<RectTransform>();
            if (bar == null || host == null) return _top;

            Graphic[] graphics = bar.GetComponentsInChildren<Graphic>(false);
            float lowest = float.MaxValue;

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (!graphic.enabled || graphic.color.a <= 0.01f) continue;

                graphic.rectTransform.GetWorldCorners(Corners);
                lowest = Mathf.Min(lowest, Corners[0].y);
            }

            if (lowest == float.MaxValue) return _top;

            // Our pills hang from the canvas top, so the bar's bottom edge has to come back in those units.
            float local = host.InverseTransformPoint(new Vector3(0f, lowest, 0f)).y;
            float measured = host.rect.height * 0.5f - local + Gap;

            if (measured >= MinTop && measured <= MaxTop) _top = measured;

            return _top;
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
            if ((_attached && GameFont.Current != null) || Time.time < _nextSearchAt) return;

            _nextSearchAt = Time.time + 1f;

            if (!_attached) Attach();
            if (GameFont.Current == null) AdoptGameFont();
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
            if (!GameFont.Find()) return;

            GameFont.Apply(_size.Label);
            GameFont.Apply(_stage.Label);
            GameFont.Apply(_hint.Label);
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
            _size = BuildPill("Size", panelled: true);
            _stage = BuildPill("Stage", panelled: true);
            _hint = BuildPill("Hint", panelled: false);
        }

        private static Pill BuildPill(string name, bool panelled)
        {
            var pill = new Pill { Object = new GameObject(GameFont.Mine + name) };
            pill.Object.transform.SetParent(_object.transform, false);

            pill.Rect = pill.Object.AddComponent<RectTransform>();
            pill.Rect.anchorMin = pill.Rect.anchorMax = new Vector2(0.5f, 1f);
            pill.Rect.pivot = new Vector2(0.5f, 1f);

            // Opts out of any layout group the game's canvas puts on its children.
            pill.Object.AddComponent<LayoutElement>().ignoreLayout = true;

            var background = pill.Object.AddComponent<Image>();
            background.color = panelled ? Panel : Color.clear;
            background.raycastTarget = false;

            var label = new GameObject("Label");
            label.transform.SetParent(pill.Object.transform, false);

            Stretch(label.AddComponent<RectTransform>());

            pill.Label = label.AddComponent<TextMeshProUGUI>();
            pill.Label.fontSize = panelled ? FontSize : HintFontSize;
            pill.Label.color = panelled ? Color.white : HintColour;
            pill.Label.alignment = TextAlignmentOptions.Center;
            pill.Label.enableWordWrapping = false;
            pill.Label.raycastTarget = false;

            GameFont.Apply(pill.Label);

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
