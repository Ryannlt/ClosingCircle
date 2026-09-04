using ClosingCircle.Core;
using UnityEngine;

// Tells the game a UI has focus, using the game's own switch for it.
//
// `set shouldUnlockMouse true` is a client console command that sets ClientGlobalVariables.ShouldUnlockMouse,
// and the game gates far more than the cursor on that flag: LocalPlayerInputManager refuses all player input
// while it is set, OwnerCameraManager stops the look, and OwnerWeaponHolder blocks striking, blocking, stance
// and weapon switching. It is exactly what the escape menu and the chat pane rely on.
//
// This is what fixes typing in the panel driving the character. Rewired has no notion of a mod's uGUI having
// focus, so before this the only safe place to type was over the escape menu.
//
// One shared boolean, not a counter, and nothing in the game writes it except this command, so the mod is its
// only writer. The failure mode is a player left unable to move, which is why Reset clears it unconditionally.

namespace ClosingCircle.Systems
{
    // Rides on the panel's own GameObject so Unity's teardown releases the flag when nothing of ours is left
    // to do it: a disconnect unloads the scene, and there is no client-side callback telling a mod its player
    // is leaving. Best effort by nature, since the console may already be gone by then.
    public class InputFocusGuard : MonoBehaviour
    {
        private void OnDestroy()
        {
            try
            {
                InputFocus.Release();
            }
            catch (System.Exception)
            {
                // Teardown order is not ours to rely on. Failing here would only add noise to a client that
                // is already leaving.
            }
        }
    }

    public static class InputFocus
    {
        private const string Command = "set shouldUnlockMouse";

        private static bool _held;

        public static void Take()
        {
            if (_held) return;

            _held = true;
            GameFacade.Execute($"{Command} true", logResult: false);
        }

        public static void Release()
        {
            if (!_held) return;

            _held = false;
            GameFacade.Execute($"{Command} false", logResult: false);
        }

        // Unconditional, and that matters. The assembly reloads on every map change and takes _held with it,
        // so a panel left open across a round change would strand the flag set with nothing left to remember
        // it was ours. Clearing on every round start costs one quiet command and cannot leave anyone stuck.
        public static void Reset()
        {
            _held = false;
            GameFacade.Execute($"{Command} false", logResult: false);
        }
    }
}
