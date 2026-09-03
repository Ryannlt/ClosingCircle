using ClosingCircle.Domain;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// The wire format for pushing a zone to clients, and the chunking that gets it through a chat message.
// Pure and Unity-free apart from Vector2, so every branch is covered by Editor tests.

namespace ClosingCircle.Sync
{
    public static class PlanCodec
    {
        public const string Marker = "[CC]";
        public const int Version = 3;

        // Conservative. The client chat field caps at 255 and the server path is unmeasured, so this leaves
        // room for the header and a wide margin rather than discovering the real cap during an event.
        public const int MaxChunkLength = 100;

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Encode(ZoneState state)
        {
            var sb = new StringBuilder();
            sb.Append(Version);
            sb.Append("|fl:").Append(state.Enabled ? 1 : 0).Append(',').Append(state.Solid ? 1 : 0)
              .Append(',').Append(state.Hud ? 1 : 0);
            sb.Append("|sh:").Append(state.Sides).Append(',').Append(N(state.Rotation));
            sb.Append("|st:").Append(N(state.StartRadius)).Append(',')
              .Append(N(state.StartCentre.x)).Append(',').Append(N(state.StartCentre.y));
            sb.Append("|lk:").Append(state.ColorR).Append(',').Append(state.ColorG).Append(',')
              .Append(state.ColorB).Append(',').Append(N(state.OpacityPercent)).Append(',')
              .Append(N(state.Height)).Append(',').Append(N(state.Fade));

            if (state.Stages != null)
            {
                foreach (Stage stage in state.Stages)
                {
                    sb.Append("|s:").Append(N(stage.FromTime)).Append(',').Append(N(stage.ToTime)).Append(',')
                      .Append(N(stage.Radius)).Append(',').Append(N(stage.Centre.x)).Append(',')
                      .Append(N(stage.Centre.y)).Append(',').Append(stage.Resolved ? 1 : 0);
                }
            }

            return sb.ToString();
        }

        public static bool TryDecode(string payload, out ZoneState state)
        {
            state = new ZoneState { Stages = new List<Stage>() };
            if (string.IsNullOrEmpty(payload)) return false;

            string[] fields = payload.Split('|');
            if (!int.TryParse(fields[0], NumberStyles.Integer, Invariant, out int version) || version != Version)
                return false;

            for (int i = 1; i < fields.Length; i++)
            {
                string[] pair = fields[i].Split(new[] { ':' }, 2);
                if (pair.Length != 2) return false;

                string[] v = pair[1].Split(',');

                switch (pair[0])
                {
                    case "fl":
                        if (v.Length != 3) return false;
                        state.Enabled = v[0] == "1";
                        state.Solid = v[1] == "1";
                        state.Hud = v[2] == "1";
                        break;

                    case "sh":
                        if (v.Length != 2) return false;
                        if (!int.TryParse(v[0], NumberStyles.Integer, Invariant, out state.Sides)) return false;
                        if (!F(v[1], out state.Rotation)) return false;
                        if (state.Sides < ZoneShape.MinSides) return false;
                        break;

                    case "st":
                        if (v.Length != 3) return false;
                        if (!F(v[0], out state.StartRadius)) return false;
                        if (!F(v[1], out float sx) || !F(v[2], out float sz)) return false;
                        state.StartCentre = new Vector2(sx, sz);
                        break;

                    case "lk":
                        if (v.Length != 6) return false;
                        if (!int.TryParse(v[0], NumberStyles.Integer, Invariant, out state.ColorR)) return false;
                        if (!int.TryParse(v[1], NumberStyles.Integer, Invariant, out state.ColorG)) return false;
                        if (!int.TryParse(v[2], NumberStyles.Integer, Invariant, out state.ColorB)) return false;
                        if (!F(v[3], out state.OpacityPercent)) return false;
                        if (!F(v[4], out state.Height)) return false;
                        if (!F(v[5], out state.Fade)) return false;
                        break;

                    case "s":
                        if (v.Length != 6) return false;
                        var stage = new Stage();
                        if (!F(v[0], out stage.FromTime)) return false;
                        if (!F(v[1], out stage.ToTime)) return false;
                        if (!F(v[2], out stage.Radius)) return false;
                        if (!F(v[3], out float cx) || !F(v[4], out float cz)) return false;
                        stage.Centre = new Vector2(cx, cz);

                        // Carried so the preview can tell a decided centre from one still to be rolled.
                        stage.Resolved = v[5] == "1";

                        if (!stage.IsValid) return false;
                        state.Stages.Add(stage);
                        break;

                    default:
                        // Unknown field from a newer build. Skipping keeps an old client usable rather than
                        // blanking its zone.
                        break;
                }
            }

            return state.Sides >= ZoneShape.MinSides;
        }

        // Header is <marker><sequence>.<index>/<count>: so a reassembler can tell a resend from a lost part.
        public static string[] Chunk(string payload, int sequence)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt((float)payload.Length / MaxChunkLength));
            var parts = new string[count];

            for (int i = 0; i < count; i++)
            {
                int start = i * MaxChunkLength;
                int length = Mathf.Min(MaxChunkLength, payload.Length - start);
                parts[i] = $"{Marker}{sequence}.{i}/{count}:{payload.Substring(start, length)}";
            }

            return parts;
        }

        public static bool TryReadChunk(string message, out int sequence, out int index, out int count,
                                        out string body)
        {
            sequence = index = count = 0;
            body = null;

            if (string.IsNullOrEmpty(message) || !message.StartsWith(Marker)) return false;

            // Split on the first colon only: the payload is full of them.
            int colon = message.IndexOf(':');
            if (colon < 0) return false;

            string header = message.Substring(Marker.Length, colon - Marker.Length);
            body = message.Substring(colon + 1);

            string[] halves = header.Split('.');
            if (halves.Length != 2) return false;

            string[] fraction = halves[1].Split('/');
            if (fraction.Length != 2) return false;

            return int.TryParse(halves[0], NumberStyles.Integer, Invariant, out sequence)
                   && int.TryParse(fraction[0], NumberStyles.Integer, Invariant, out index)
                   && int.TryParse(fraction[1], NumberStyles.Integer, Invariant, out count)
                   && count > 0 && index >= 0 && index < count;
        }

        private static string N(float value) => value.ToString("0.###", Invariant);

        private static bool F(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, Invariant, out value);
    }
}
