using ClosingCircle.Core;
using UMod;
using UnityEngine;

// The only file that touches the uMod API, kept alone on purpose the way ClientPrefs is: if the security
// verifier refuses it, this is the single file to remove.
//
// It exists because a mod's assets are NOT reachable by Resources.Load. That reads the game's own
// resources.assets; a mod's live in its sharedassets bundle, and IModAssets is the only door to them. Assuming
// otherwise is what left the zone wall silently falling back to URP Unlit with no blur for two builds.

namespace ClosingCircle.Systems
{
    public static class ModAssets
    {
        private static IModAssets _assets;
        private static bool _listed;

        public static bool TryLoad<T>(string name, out T asset) where T : Object
        {
            asset = null;

            IModAssets assets = Resolve();
            if (assets == null || !assets.CanLoadAssets) return false;

            List(assets);

            // Exists first, because Load on a missing name is not promised to be quiet.
            if (!assets.Exists(name)) return false;

            asset = assets.Load<T>(name);
            return asset != null;
        }

        private static IModAssets Resolve()
        {
            if (_assets != null) return _assets;

            // The Type overload, never the parameterless one: that walks the stack with StackFrame.GetMethod(),
            // and System.Reflection is on the uMod deny list. Passing a type keeps our own IL clean of it.
            IModContext context = ModContextProvider.GetContext(typeof(ModAssets));

            if (context == null)
            {
                Logger.Log("uMod gave this mod no context, so its own assets cannot be loaded.",
                           LogLevel.WARNING);
                return null;
            }

            _assets = context.ModAssets;
            return _assets;
        }

        // Logged once, because the addressing scheme for a bundled asset is not documented anywhere we can
        // read: this is how we find out whether the key is a bare name or a full project path.
        private static void List(IModAssets assets)
        {
            if (_listed) return;
            _listed = true;

            string[] names = assets.FindAllNames();
            if (names == null) return;

            Logger.Log($"This mod ships {names.Length} asset(s): {string.Join(", ", names)}", LogLevel.DEBUG);
        }
    }
}
