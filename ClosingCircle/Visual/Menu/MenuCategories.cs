using ClosingCircle.Core;
using ClosingCircle.Domain;
using ClosingCircle.Systems;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// What each category contains. Display writes the player's own settings and sends nothing; the rest emit an rc
// command through MenuCommands, so the panel has no authority the admin did not already have by typing.

namespace ClosingCircle.Visual.Menu
{
    public static class MenuCategories
    {
        private static readonly string[] Shapes =
            { "Circle", "Triangle", "Square", "Pentagon", "Hexagon", "Heptagon", "Octagon" };

        private static readonly string[] Modes = { "Bisector", "Random", "Players", "Fixed" };

        // Zone settings reach everyone, so they are staged here and sent only on Apply. Static so a push
        // landing mid-edit rebuilds the tab without throwing away what the admin was in the middle of typing.
        private static readonly Dictionary<string, object> Pending = new Dictionary<string, object>();

        // Held outside the widget for the same reason the staged edits are: PreviewCommand pushes the plan to
        // the caller, that push rebuilds this tab, and a checkbox that owned its own state would come back
        // unticked every time. It then only ever sent 'on'.
        private static bool _previewOn;

        private static Button _apply;
        private static Button _revert;
        private static TextMeshProUGUI _pendingNote;

        private static ColourWheel _wheel;
        private static Slider[] _channels;
        private static TextMeshProUGUI[] _channelValues;
        private static Image _swatch;
        private static bool _syncing;

        /* ---------------- Display ---------------- */

        public static void BuildDisplay(Transform parent)
        {
            Head(parent, "Display", ClientDisplay.Forced
                     ? "The server is forcing its own circle"
                     : "Display settings are client side only");

            if (ClientDisplay.Forced)
                Note(parent, "This server draws its own circle for everyone. Your settings are kept and come " +
                             "back on a server that does not force one.");

            DisplaySettings look = ClientDisplay.Effective();

            BuildColour(parent, look.Colour);

            Slider opacity = null, height = null, fade = null, blur = null;

            opacity = Row(parent, "Opacity", DisplayCodec.MinOpacityPercent, 100f, look.OpacityPercent,
                          "0", v => Store(look.Colour, opacity, height, fade, blur));

            height = Row(parent, "Wall height", DisplayCodec.MinHeight, DisplayCodec.MaxHeight, look.Height,
                         "0", v => Store(look.Colour, opacity, height, fade, blur));

            fade = Row(parent, "Fade", 0f, 100f, look.FadePercent,
                       "0", v => Store(look.Colour, opacity, height, fade, blur));

            blur = Row(parent, "Blur", 0f, 100f, look.BlurPercent,
                       "0", v => Store(look.Colour, opacity, height, fade, blur));

            Note(parent, "Refracts what is behind the wall. Turn it down if performance is effected.");

            GameObject hudRow = LineRow(parent);
            Caption(hudRow.transform, "Show HUD", 230f);
            MenuWidgets.Check(hudRow.transform, look.Hud, on =>
            {
                DisplaySettings now = ClientDisplay.Effective();
                now.Hud = on;
                ClientDisplay.Apply(now);
            });

            Note(parent, "Circle size and stage countdown, top centre. Hidden for everyone if the server has " +
                         "turned it off in config.");

            GameObject buttons = LineRow(parent);
            MenuWidgets.Press(buttons.transform, "Reset to server", () =>
            {
                ClientDisplay.ResetToServer();
                ZonePanel.Refresh();
            }, width: 260f);

            BuildSettingsString(parent);
        }

        private static void BuildColour(Transform parent, Color colour)
        {
            GameObject row = LineRow(parent, 240f);

            GameObject wheelHost = MenuWidgets.Node("Wheel", row.transform);
            MenuWidgets.Size(wheelHost, 210f, 210f);

            Image face = MenuWidgets.Panel(wheelHost, Color.white);
            face.sprite = ColourWheel.Sprite();
            face.type = Image.Type.Simple;

            GameObject markerHost = MenuWidgets.Node("Marker", wheelHost.transform);
            RectTransform marker = markerHost.GetComponent<RectTransform>();
            marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.sizeDelta = new Vector2(16f, 16f);
            Image markerFill = MenuWidgets.Panel(markerHost, colour);
            markerFill.raycastTarget = false;

            _wheel = wheelHost.AddComponent<ColourWheel>();
            _wheel.Bind(wheelHost.GetComponent<RectTransform>(), marker, markerFill);

            GameObject channels = MenuWidgets.Node("Channels", row.transform);
            MenuWidgets.Size(channels, 510f, 230f);
            MenuWidgets.Column(channels, 8f, new RectOffset(16, 0, 8, 0));

            GameObject swatchRow = LineRow(channels.transform);
            GameObject swatchHost = MenuWidgets.Node("Swatch", swatchRow.transform);
            MenuWidgets.Size(swatchHost, 30f, 30f);
            _swatch = MenuWidgets.Panel(swatchHost, colour);

            TextMeshProUGUI readout = MenuWidgets.Text(swatchRow.transform, Channels(colour),
                                                       MenuWidgets.Small, MenuWidgets.Dim);
            MenuWidgets.Size(readout.gameObject, 200f, 30f);

            _channels = new Slider[3];
            _channelValues = new TextMeshProUGUI[3];
            string[] names = { "R", "G", "B" };
            float[] start = { colour.r * 255f, colour.g * 255f, colour.b * 255f };

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                GameObject line = LineRow(channels.transform);
                Caption(line.transform, names[i], 30f);

                _channels[i] = MenuWidgets.Bar(line.transform, 0f, 255f, start[i], v => FromChannels(readout));
                _channelValues[i] = MenuWidgets.Text(line.transform, Mathf.RoundToInt(start[i]).ToString(),
                                                     MenuWidgets.Small, MenuWidgets.Dim);
                MenuWidgets.Size(_channelValues[i].gameObject, 50f, 28f);
            }

            // Brightness sits with the wheel rather than the channels, because it is the third axis of the
            // same pick and not a fourth channel.
            GameObject valueRow = LineRow(channels.transform);
            Caption(valueRow.transform, "Bright", 90f);
            float v0;
            float h0, s0;
            Color.RGBToHSV(colour, out h0, out s0, out v0);
            MenuWidgets.Bar(valueRow.transform, 0f, 100f, v0 * 100f, v => _wheel.SetValue(v / 100f));

            _wheel.Show(colour);
            _wheel.Picked = picked => FromWheel(picked, readout);
        }

        // One colour is the source of truth and both controls write it, with a guard so echoing a change back
        // into the other cannot loop.
        private static void FromWheel(Color colour, TextMeshProUGUI readout)
        {
            if (_syncing) return;
            _syncing = true;

            for (int i = 0; i < 3; i++)
            {
                float channel = i == 0 ? colour.r : i == 1 ? colour.g : colour.b;
                _channels[i].SetValueWithoutNotify(channel * 255f);
                _channelValues[i].text = Mathf.RoundToInt(channel * 255f).ToString();
            }

            Paint(colour, readout);
            _syncing = false;
        }

        private static void FromChannels(TextMeshProUGUI readout)
        {
            if (_syncing) return;
            _syncing = true;

            var colour = new Color(_channels[0].value / 255f, _channels[1].value / 255f,
                                   _channels[2].value / 255f);

            for (int i = 0; i < 3; i++) _channelValues[i].text = Mathf.RoundToInt(_channels[i].value).ToString();

            _wheel.Show(colour);
            Paint(colour, readout);
            _syncing = false;
        }

        private static void Paint(Color colour, TextMeshProUGUI readout)
        {
            if (_swatch != null) _swatch.color = colour;
            if (readout != null) readout.text = Channels(colour);

            DisplaySettings now = ClientDisplay.Effective();
            now.Colour = colour;
            ClientDisplay.Apply(now);
        }

        private static void Store(Color colour, Slider opacity, Slider height, Slider fade, Slider blur)
        {
            DisplaySettings now = ClientDisplay.Effective();

            if (opacity != null) now.OpacityPercent = opacity.value;
            if (height != null) now.Height = height.value;
            if (fade != null) now.FadePercent = fade.value;
            if (blur != null) now.BlurPercent = blur.value;

            ClientDisplay.Apply(now);
        }

        private static void BuildSettingsString(Transform parent)
        {
            Note(parent, "Settings are saved between rounds and servers. Import or export settings from a friend.");

            TextMeshProUGUI result = null;

            GameObject exportRow = LineRow(parent);
            TMP_InputField export = MenuWidgets.Field(exportRow.transform, ClientDisplay.Export(), 440f, null);

            // systemCopyBuffer rather than a select-and-copy, since the panel eats the keyboard anyway.
            MenuWidgets.Press(exportRow.transform, "Copy", () =>
            {
                GUIUtility.systemCopyBuffer = export.text;

                result.text = "Copied to the clipboard.";
                result.color = MenuWidgets.Ok;
            });

            TMP_InputField import = null;

            GameObject importRow = LineRow(parent);
            import = MenuWidgets.Field(importRow.transform, string.Empty, 440f, null);

            MenuWidgets.Press(importRow.transform, "Paste", () => import.text = GUIUtility.systemCopyBuffer);

            MenuWidgets.Press(importRow.transform, "Import", () =>
            {
                string problem;
                bool worked = ClientDisplay.Import(import.text, out problem);

                result.text = worked ? "Settings applied." : problem;
                result.color = worked ? MenuWidgets.Ok : MenuWidgets.Accent;

                if (worked) ZonePanel.Refresh();
            });

            result = MenuWidgets.Text(parent, string.Empty, MenuWidgets.Tiny, MenuWidgets.Faint);
        }

        /* ---------------- Zone ---------------- */

        public static void BuildZone(Transform parent)
        {
            Head(parent, "Zone", "Sends rc closingCircle set");

            Toggle(parent, "Circle enabled", ZoneService.Enabled, "EnableCircle");
            Toggle(parent, "Solid wall", ZoneService.Solid, "Solid");
            Toggle(parent, "Announce stages", ZoneService.Announce, "Announce");
            Toggle(parent, "Force server look", ZoneService.ForceDisplay, "ForceDisplay");

            GameObject shapeRow = LineRow(parent);
            Caption(shapeRow.transform, "Shape", 230f, Dirty("Shape"));
            string shape = Staged("Shape", ZoneShape.Name(ZoneService.Shape.Sides));
            MenuWidgets.Choose(shapeRow.transform, Shapes, IndexOf(Shapes, shape), 200f,
                              value => Stage("Shape", value));

            Admin(parent, "Start radius", ZoneService.Plan.StartRadius, "StartRadius");
            Admin(parent, "Damage", ZoneService.Damage, "Damage");
            Admin(parent, "Spread", ZoneService.Spread, "Spread");
            Admin(parent, "Rotation", ZoneService.Shape.Rotation, "Rotation");

            GameObject bisectorRow = LineRow(parent);
            Caption(bisectorRow.transform, "Bisector", 230f, Dirty("Bisector"));
            MenuWidgets.Field(bisectorRow.transform, Staged("Bisector", Bisector()), 260f,
                              value => Stage("Bisector", value));

            GameObject centreRow = LineRow(parent);
            Caption(centreRow.transform, "Start centre", 230f, Dirty("StartCentre"));
            MenuWidgets.Field(centreRow.transform, Staged("StartCentre", Point(ZoneService.Plan.StartCentre)),
                              260f, value => Stage("StartCentre", value));

            BuildConfirm(parent);
        }

        // Nothing above reaches the server until this does. A slider drag would otherwise send a command per
        // frame, and every one of these settings is seen by everybody on the server.
        private static void BuildConfirm(Transform parent)
        {
            GameObject row = LineRow(parent);

            _apply = MenuWidgets.Press(row.transform, "Apply", Apply, primary: true, width: 170f);
            _revert = MenuWidgets.Press(row.transform, "Revert", Revert, width: 170f);

            _pendingNote = MenuWidgets.Text(row.transform, string.Empty, MenuWidgets.Tiny, MenuWidgets.Dim);
            MenuWidgets.Size(_pendingNote.gameObject, 380f, 30f);

            MarkPending();
        }

        private static void Apply()
        {
            if (Pending.Count == 0) return;

            // One command for the whole batch, so the server answers once and the strip below shows exactly
            // what went out rather than only the last of several lines.
            var pairs = new List<KeyValuePair<string, string>>();

            foreach (KeyValuePair<string, object> change in Pending)
                pairs.Add(new KeyValuePair<string, string>(change.Key, MenuCommands.Value(change.Value)));

            Send(MenuCommands.Set(pairs));

            Pending.Clear();
            ZonePanel.Refresh();
        }

        private static void Revert()
        {
            if (Pending.Count == 0) return;

            Pending.Clear();
            ZonePanel.Refresh();
        }

        private static void Stage(string key, object value)
        {
            Pending[key] = value;
            MarkPending();
        }

        private static void MarkPending()
        {
            MenuWidgets.Enable(_apply, Pending.Count > 0);
            MenuWidgets.Enable(_revert, Pending.Count > 0);

            if (_pendingNote == null) return;

            _pendingNote.text = Pending.Count == 0
                ? "No unsent changes."
                : Pending.Count == 1 ? "1 change waiting to be sent." : $"{Pending.Count} changes waiting to be sent.";

            _pendingNote.color = Pending.Count == 0 ? MenuWidgets.Dim : MenuWidgets.Accent;
        }

        public static void ClearPending()
        {
            Pending.Clear();

            // The server drops the preview with the round, so the toggle has to agree.
            _previewOn = false;
        }

        private static bool Dirty(string key) => Pending.ContainsKey(key);

        private static string Staged(string key, string fallback)
        {
            object value;
            return Pending.TryGetValue(key, out value) ? value.ToString() : fallback;
        }

        private static float Staged(string key, float fallback)
        {
            object value;
            return Pending.TryGetValue(key, out value) && value is float number ? number : fallback;
        }

        private static bool Staged(string key, bool fallback)
        {
            object value;
            return Pending.TryGetValue(key, out value) && value is bool flag ? flag : fallback;
        }

        private static int IndexOf(string[] options, string value)
        {
            for (int i = 0; i < options.Length; i++)
                if (string.Equals(options[i], value, System.StringComparison.OrdinalIgnoreCase)) return i;

            return 0;
        }

        /* ---------------- Stages ---------------- */

        public static void BuildStages(Transform parent)
        {
            Head(parent, "Stages", "Times are seconds remaining, so from is the larger number");

            ZonePlan plan = ZoneService.Plan;

            if (plan.Count == 0) Note(parent, "No stages. The zone holds its start size all round.");

            for (int i = 0; i < plan.Count; i++)
            {
                int index = i;
                Stage stage = plan.Stages[i];

                GameObject row = LineRow(parent);
                Caption(row.transform, $"[{i}]  {stage.FromTime:0}s to {stage.ToTime:0}s", 290f);
                Caption(row.transform, $"{stage.Radius:0}m", 100f);
                Caption(row.transform, stage.Mode.ToString(), 140f);

                MenuWidgets.Press(row.transform, "Remove", () =>
                {
                    Send(MenuCommands.RemoveStage(index));
                    ZonePanel.Refresh();
                });
            }

            Note(parent, "Add a stage");

            GameObject builder = LineRow(parent);
            TMP_InputField from = MenuWidgets.Field(builder.transform, "540", 120f, null);
            TMP_InputField to = MenuWidgets.Field(builder.transform, "480", 120f, null);
            TMP_InputField radius = MenuWidgets.Field(builder.transform, "100", 120f, null);

            string mode = Modes[0];
            MenuWidgets.Choose(builder.transform, Modes, 0, 170f, value => mode = value);

            TextMeshProUGUI problems = MenuWidgets.Text(parent, string.Empty, MenuWidgets.Tiny,
                                                        MenuWidgets.Accent);
            problems.enableWordWrapping = true;

            MenuWidgets.Press(builder.transform, "Add", () =>
            {
                float f, t, r;
                if (!float.TryParse(from.text, out f) || !float.TryParse(to.text, out t) ||
                    !float.TryParse(radius.text, out r))
                {
                    problems.text = "From, to and radius all have to be numbers.";
                    return;
                }

                // The server's own validator, run here first, so an illegal stage is refused before it is sent
                // rather than warned about after.
                string refusal = Refuse(f, t, r);
                if (refusal != null)
                {
                    problems.text = refusal;
                    return;
                }

                problems.text = string.Empty;
                Send(MenuCommands.AddStage(f, t, r, Mode(mode)));
                ZonePanel.Refresh();
            }, primary: true);

            GameObject actions = LineRow(parent);

            Button clear = null;
            bool armed = false;

            clear = MenuWidgets.Press(actions.transform, "Clear all", () =>
            {
                // Two presses, because this throws away the whole plan and there is no undo. A press is
                // otherwise a decision in itself, which is why nothing else here asks twice.
                if (!armed)
                {
                    armed = true;
                    Label(clear, "Clear all? press again");
                    return;
                }

                Send(MenuCommands.ClearStages());
                ZonePanel.Refresh();
            }, width: 300f);
        }

        private static void Label(Button button, string text)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text.ToUpperInvariant();
                label.color = MenuWidgets.Accent;
            }
        }

        // Builds the plan the stage would produce and asks the real validator about it, so the panel and the
        // server can never disagree on what is legal.
        private static string Refuse(float from, float to, float radius)
        {
            ZonePlan plan = ZoneService.Plan;
            var trial = new ZonePlan { StartRadius = plan.StartRadius, StartCentre = plan.StartCentre };

            for (int i = 0; i < plan.Count; i++) trial.Add(plan.Stages[i]);

            trial.Add(new Stage
            {
                FromTime = from, ToTime = to, Radius = radius, Mode = CentreMode.Bisector
            });

            var context = new ValidationContext
            {
                HasBisector = ZoneService.HasBisector,
                BisectorPoint = ZoneService.BisectorPoint,
                BisectorHeading = ZoneService.BisectorHeading,
                Solid = ZoneService.Solid,
                Damage = ZoneService.Damage,
                RoundSeconds = ZoneService.RoundSeconds
            };

            System.Collections.Generic.List<Finding> findings = PlanValidator.Validate(trial, context);

            for (int i = 0; i < findings.Count; i++)
            {
                if (findings[i].Level == FindingLevel.Note) continue;

                return findings[i].Message;
            }

            return null;
        }

        /* ---------------- Actions ---------------- */

        public static void BuildActions(Transform parent)
        {
            Head(parent, "Actions", "Replies arrive as a private message");

            GameObject row = LineRow(parent);
            MenuWidgets.Press(row.transform, "Preview 60s", () => Send(MenuCommands.Preview(60f)));
            MenuWidgets.Press(row.transform, "Validate", () => Send(MenuCommands.Validate()));
            MenuWidgets.Press(row.transform, "Status", () => Send(MenuCommands.Status()));
            MenuWidgets.Press(row.transform, "Re-send", () => Send(MenuCommands.Push()));

            GameObject advance = LineRow(parent);

            Button skip = null;
            bool armed = false;

            skip = MenuWidgets.Press(advance.transform, "Advance to next stage", () =>
            {
                // Two presses, like Clear all: it moves the round along for everybody and cannot be undone.
                if (!armed)
                {
                    armed = true;
                    Label(skip, "Advance? press again");
                    return;
                }

                Send(MenuCommands.AdvanceStage());
                ZonePanel.Refresh();
            }, width: 320f);

            Note(parent, "Starts the next stage now and brings every stage after it forward by the same " +
                         "amount, so the gaps between them are unchanged.");

            GameObject hold = LineRow(parent);
            Caption(hold.transform, "Keep preview on", 280f, _previewOn);
            MenuWidgets.Check(hold.transform, _previewOn, on =>
            {
                _previewOn = on;
                Send(MenuCommands.Preview(on));
            });
        }

        /* ---------------- Locked ---------------- */

        public static void BuildLocked(Transform parent)
        {
            Head(parent, "Admin tools locked", string.Empty);

            Note(parent, "These tools are available to server admins. Unlocking asks the server whether you " +
                         "are one. If you are whitelisted it will simply work, with nothing typed.");

            GameObject row = LineRow(parent);
            MenuWidgets.Press(row.transform, "Unlock admin tools", () =>
            {
                AdminAccess.Probe();
                ZonePanel.Sends(MenuCommands.Whoami());
            }, primary: true, width: 300f);

            if (!AdminAccess.Refused) return;

            Note(parent, "No reply, so this server does not consider you an admin. If it uses a password, " +
                         "enter it here.");

            GameObject login = LineRow(parent);
            TMP_InputField password = MenuWidgets.Field(login.transform, string.Empty, 320f, null);
            password.contentType = TMP_InputField.ContentType.Password;
            password.onSubmit.AddListener(value => AdminAccess.Login(value));

            MenuWidgets.Press(login.transform, "Log in", () => AdminAccess.Login(password.text));

            if (string.IsNullOrEmpty(AdminAccess.LoginProblem)) return;

            TextMeshProUGUI problem = MenuWidgets.Text(parent, AdminAccess.LoginProblem, MenuWidgets.Tiny,
                                                       MenuWidgets.Accent);
            problem.enableWordWrapping = true;
            MenuWidgets.Size(problem.gameObject, 0f, 40f);
        }

        /* ---------------- shared bits ---------------- */

        private static void Head(Transform parent, string title, string scope)
        {
            GameObject row = LineRow(parent);

            TextMeshProUGUI heading = MenuWidgets.Text(row.transform, title.ToUpperInvariant(),
                                                       MenuWidgets.Small, MenuWidgets.Dim);
            heading.characterSpacing = 10f;
            MenuWidgets.Size(heading.gameObject, 0f, 30f).flexibleWidth = 1f;

            if (!string.IsNullOrEmpty(scope))
            {
                TextMeshProUGUI note = MenuWidgets.Text(row.transform, scope, MenuWidgets.Tiny,
                                                        MenuWidgets.Faint, TextAlignmentOptions.Right);
                MenuWidgets.Size(note.gameObject, 520f, 30f);
            }

            GameObject rule = MenuWidgets.Node("Rule", parent);
            MenuWidgets.Panel(rule, MenuWidgets.Line);
            MenuWidgets.Size(rule, 0f, 1f);
        }

        // The height matters: a LayoutElement beats what the children want, so a row holding something taller
        // than it says is centred in its own band and spills over its neighbours in both directions.
        private static GameObject LineRow(Transform parent, float height = 38f)
        {
            GameObject row = MenuWidgets.Node("Row", parent);
            MenuWidgets.Line_(row, 14f, new RectOffset(0, 0, 0, 0));
            MenuWidgets.Size(row, 0f, height);
            return row;
        }

        private static TextMeshProUGUI Caption(Transform parent, string text, float width, bool pending = false)
        {
            TextMeshProUGUI label = MenuWidgets.Text(parent, text, MenuWidgets.Body,
                                                     pending ? MenuWidgets.Accent : MenuWidgets.Ink);
            MenuWidgets.Size(label.gameObject, width, 30f);
            return label;
        }

        private static void Note(Transform parent, string text)
        {
            TextMeshProUGUI label = MenuWidgets.Text(parent, text, MenuWidgets.Tiny, MenuWidgets.Faint);
            label.enableWordWrapping = true;
            MenuWidgets.Size(label.gameObject, 0f, 40f);
        }

        private static Slider Row(Transform parent, string title, float min, float max, float value,
                                  string format, System.Action<float> changed, bool pending = false)
        {
            GameObject row = LineRow(parent);
            Caption(row.transform, title, 230f, pending);

            TextMeshProUGUI readout = null;
            Slider bar = MenuWidgets.Bar(row.transform, min, max, value, v =>
            {
                readout.text = v.ToString(format);
                if (changed != null) changed(v);
            });

            readout = MenuWidgets.Text(row.transform, value.ToString(format), MenuWidgets.Small,
                                       MenuWidgets.Dim);
            MenuWidgets.Size(readout.gameObject, 64f, 30f);

            return bar;
        }

        // Bounds come from SettingRanges, which is also what the server validates against, so the slider
        // cannot offer a value that would be refused. Shown units and wire units differ for Spread and Fade.
        private static void Admin(Transform parent, string title, float wire, string key)
        {
            SettingRange range = SettingRanges.Get(key);
            float shown = range.Shown(Staged(key, wire));

            Row(parent, title, range.SliderMin, range.SliderMax, shown, "0",
                v => Stage(key, range.Wire(v)), Dirty(key));
        }

        private static void Toggle(Transform parent, string title, bool on, string key)
        {
            GameObject row = LineRow(parent);
            Caption(row.transform, title, 230f, Dirty(key));
            MenuWidgets.Check(row.transform, Staged(key, on), value => Stage(key, value));
        }

        private static void Send(string command)
        {
            GameFacade.Execute(command, logResult: false);
            ZonePanel.Sends(command);
        }

        private static CentreMode Mode(string name)
        {
            if (name == "Random") return CentreMode.Random;
            if (name == "Players") return CentreMode.Players;
            if (name == "Fixed") return CentreMode.Fixed;

            return CentreMode.Bisector;
        }

        private static string Channels(Color colour) =>
            $"{Mathf.RoundToInt(colour.r * 255f)}, {Mathf.RoundToInt(colour.g * 255f)}, " +
            $"{Mathf.RoundToInt(colour.b * 255f)}";

        private static string Point(Vector2 point) => $"{point.x:0.#},{point.y:0.#}";

        private static string Bisector() =>
            ZoneService.HasBisector
                ? $"{ZoneService.BisectorPoint.x:0.#},{ZoneService.BisectorPoint.y:0.#}," +
                  $"{ZoneService.BisectorHeading:0.#}"
                : "0,0,90";
    }
}
