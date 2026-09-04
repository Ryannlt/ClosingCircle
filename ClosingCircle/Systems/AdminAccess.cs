using ClosingCircle.Core;
using ClosingCircle.Domain;
using ClosingCircle.Visual.Menu;
using UnityEngine;

// Whether this client may drive the mod. Client side: the server enforces regardless, so this only decides
// whether the panel offers the controls at all.
//
// Listening for a login is not enough. IsLoggedIn on the server is true for loggedOnPlayerIDs OR anyone in
// serverAdmins.txt, and a whitelisted admin never calls RequestLogin, so OnRCLogin never fires for them even
// though every command they send works. Worse, rc login itself can only ever succeed when
// server_admin_password is set, so on a whitelist-only server that path is dead for everybody.
//
// What does work: RequestCommandExecute returns silently when IsLoggedIn is false, so the server simply never
// answers a non-admin. A reply is proof of authority and silence is the negative. That is the same fact MDS
// leans on server side, applied here on the client.

namespace ClosingCircle.Systems
{
    public static class AdminAccess
    {
        // Long enough to cover a round trip on a bad connection, short enough that a locked panel does not feel
        // broken while it waits.
        private const float ReplyWindow = 3f;

        public static bool IsAdmin { get; private set; }

        private static bool _probed;
        private static float _askedAt = -1f;

        private static float _triedAt = -1f;

        // What to tell someone whose password did not work. Held here rather than in the widget because the
        // panel rebuilds its categories on every push, which would wipe a message the control owned.
        public static string LoginProblem { get; private set; }

        // Admin rights are a session property, but this is a static and the assembly reloads every map change,
        // so the flag has to be re-established rather than remembered.
        public static void Reset()
        {
            IsAdmin = false;
            _probed = false;
            _askedAt = -1f;
            _triedAt = -1f;
            LoginProblem = null;
        }

        public static bool Waiting => _askedAt >= 0f && Time.time - _askedAt < ReplyWindow;

        // True once a probe has gone unanswered, which is how the panel knows to offer a password instead.
        public static bool Refused => _probed && !IsAdmin && _askedAt >= 0f && !Waiting;

        // Quiet and read-only. Nothing reaches chat and nothing changes, so it is safe to send unprompted.
        public static void Probe()
        {
            _probed = true;
            _askedAt = Time.time;
            GameFacade.Execute(MenuCommands.Whoami(), logResult: false);
        }

        // Once per round, so a panel opened at any point already knows the answer.
        public static void ProbeOnce()
        {
            if (_probed || IsAdmin) return;

            Probe();
        }

        public static void Login(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                Report("Enter the server's admin password first.");
                return;
            }

            LoginProblem = null;
            _triedAt = Time.time;

            GameFacade.Execute(MenuCommands.Login(password), logResult: false);
        }

        // A wrong password on a server that has one answers with a failure. A server with no password set
        // never answers at all, because RequestLogin drops out before it compares anything, so silence has to
        // be reported too or the button looks broken.
        public static void Step()
        {
            if (_triedAt < 0f || IsAdmin) return;
            if (Time.time - _triedAt < ReplyWindow) return;

            _triedAt = -1f;
            Report("No answer. This server may not use a password, in which case only the whitelist grants " +
                   "admin and there is nothing to type here.");
        }

        private static void Report(string problem)
        {
            LoginProblem = problem;

            if (ZonePanel.IsOpen) ZonePanel.Refresh();
        }

        // Any answered command proves it, whatever the command was. Granted and never revoked: a whitelisted
        // admin who mistypes a password is still an admin.
        public static void OnCommandAnswered()
        {
            if (IsAdmin) return;

            IsAdmin = true;
            Logger.Log("The server answered an rc command, so this client has admin tools.", LogLevel.DEBUG);

            // Guarded above by the early return, so this only runs on the transition: an open panel is showing
            // locked tabs that are no longer locked.
            if (ZonePanel.IsOpen) ZonePanel.Refresh();

            // An admin is exempt from ForceDisplay, so their own look is due back. Nothing else bumps the
            // version on this transition, and the next push could be minutes away.
            ZoneService.MarkLookChanged();
        }

        public static void OnLogin(bool succeeded)
        {
            _triedAt = -1f;

            if (succeeded)
            {
                LoginProblem = null;
                OnCommandAnswered();
                return;
            }

            Report("That password was not accepted.");
        }
    }
}
