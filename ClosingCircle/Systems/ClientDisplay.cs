using ClosingCircle.Domain;
using UnityEngine;

// A player's own view of the zone. Cosmetic only: colour, opacity, height, fade, blur and whether the HUD
// draws. Solid and Damage are deliberately not here, because a client that could switch those off could walk
// out of a lethal zone unharmed.
//
// These cannot be written into ZoneService, because PlanReceiver overwrites it on every push. Instead the whole
// look is held as one optional override and the visual layer asks here rather than there, so the server stays
// authoritative and a push never fights a preference.

namespace ClosingCircle.Systems
{
    public static class ClientDisplay
    {
        private const string StoreKey = "Display";
        private const string SeenPanelKey = "SeenPanel";
        private const string RoundKey = "LastRound.";

        private static int _pendingRound;
        private static string _pendingServer;

        private static bool _loaded;
        private static bool _has;
        private static DisplaySettings _mine;

        public static bool HasOverride => _has;

        // What the visual watches instead of ZoneService.LookVersion, because the look can now change from
        // either side. Both halves only ever count up, so their sum is a safe change detector.
        public static int LookVersion => ZoneService.LookVersion + _revision;

        private static int _revision;

        // Whether this player has ever opened the panel, which is what retires the hotkey hint.
        public static bool SeenPanel { get; private set; }

        public static void NotePanelOpened()
        {
            if (SeenPanel) return;

            SeenPanel = true;
            ClientPrefs.SetBool(SeenPanelKey, true);
        }

        // The hint is worth showing once per server session, not once per lifetime: someone who learned the
        // key months ago on another server should still be told it here.
        //
        // A server's round numbering restarts when the server does, and that is the only signal a client gets
        // that this is a new session rather than the next round of one it has been in all evening. Neither
        // side can hold a session id, because the mod's assembly reloads on every map change.
        public static void NoteRound(int roundId, string serverName)
        {
            // Load scopes the store by steam id, so writing before it has run would use the wrong keys.
            if (!_loaded)
            {
                _pendingRound = roundId;
                _pendingServer = serverName;
                return;
            }

            string key = RoundKey + (serverName ?? string.Empty);
            int last = ClientPrefs.GetInt(key, 0);

            ClientPrefs.SetInt(key, roundId);

            // Unknown server, or one whose numbering has gone backwards, which means it restarted.
            if (last != 0 && roundId > last) return;

            SeenPanel = false;
            ClientPrefs.SetBool(SeenPanelKey, false);
        }

        // Called once the client knows who it is, since the store is scoped per steam id.
        public static void Load(ulong steamId)
        {
            ClientPrefs.SetScope(steamId);

            _loaded = true;
            _has = false;
            SeenPanel = ClientPrefs.GetBool(SeenPanelKey, false);

            if (_pendingServer != null)
            {
                int round = _pendingRound;
                string server = _pendingServer;

                _pendingServer = null;
                NoteRound(round, server);
            }

            string stored = ClientPrefs.Get(StoreKey, string.Empty);
            if (string.IsNullOrEmpty(stored)) return;

            if (DisplayCodec.TryDecode(stored, out DisplaySettings settings, out string problem))
            {
                _mine = settings;
                _has = true;
                _revision++;
                return;
            }

            // A stored string this build cannot read is dropped rather than argued with, so an old format
            // cannot leave somebody stuck with no way back to the server's look.
            Logger.Log($"Stored display settings were discarded: {problem}", LogLevel.DEBUG);
            ClientPrefs.Clear(StoreKey);
        }

        // The server's look, which is also what a player without an override sees.
        public static DisplaySettings FromServer() => new DisplaySettings
        {
            Colour = ZoneService.Color,
            OpacityPercent = ZoneService.Opacity * 100f,
            Height = ZoneService.Height,
            FadePercent = ZoneService.Fade * 100f,
            BlurPercent = ZoneService.Blur * 100f,
            Hud = ZoneService.Hud
        };

        // The server can insist everyone sees its own circle. The override is kept rather than cleared, so a
        // player's look comes straight back on a server that does not force one.
        public static bool Forced => ZoneService.ForceDisplay && !AdminAccess.IsAdmin;

        public static DisplaySettings Effective()
        {
            DisplaySettings server = DisplayCodec.Clamp(FromServer());

            if (!_has) return server;
            if (!Forced) return _mine;

            // Forced means the server's wall, but the HUD stays the player's own business: it shows what
            // anyone can already see, so forcing it would restrict without protecting anything.
            server.Hud = _mine.Hud;
            return server;
        }

        public static void Apply(DisplaySettings settings)
        {
            _mine = DisplayCodec.Clamp(settings);
            _has = true;
            _revision++;

            if (_loaded) ClientPrefs.Set(StoreKey, DisplayCodec.Encode(_mine));
        }

        public static void ResetToServer()
        {
            _has = false;
            _revision++;

            if (_loaded) ClientPrefs.Clear(StoreKey);
        }

        public static string Export() => DisplayCodec.Encode(Effective());

        public static bool Import(string text, out string problem)
        {
            if (!DisplayCodec.TryDecode(text, out DisplaySettings settings, out problem)) return false;

            Apply(settings);
            return true;
        }

        /* What the visual layer actually asks for, so nothing else has to know an override exists. */

        public static Color Colour => Effective().Colour;

        public static Color Tinted
        {
            get
            {
                DisplaySettings look = Effective();
                Color colour = look.Colour;
                return new Color(colour.r, colour.g, colour.b, look.OpacityPercent / 100f);
            }
        }

        public static float Height => Effective().Height;

        public static float Fade => Effective().FadePercent / 100f;

        public static float Blur => Effective().BlurPercent / 100f;

        public static bool Hud => Effective().Hud;
    }
}
