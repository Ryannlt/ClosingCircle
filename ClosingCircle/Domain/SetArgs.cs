using System;

// How the arguments to 'set' are read. Pure, and separate from the command, so the one case that makes this
// awkward can be pinned by a test rather than reasoned about once and hoped for.
//
// That case: a value like 0,0,90 arrives as a single argument, but somebody typing '0, 0, 90' splits it into
// three, and the single-key form has always rejoined them. Multiple key/value pairs cannot use that rejoin, so
// the two forms have to be told apart without either stealing the other's input.

namespace ClosingCircle.Domain
{
    public static class SetArgs
    {
        // Only pairs when it cannot be anything else: an even count, more than one pair, and a known key in
        // every key position. A split value fails the key test at position 2 and falls through to the rejoin.
        public static bool LooksLikePairs(string[] args, Func<string, bool> isKey)
        {
            if (args == null || isKey == null) return false;
            if (args.Length < 4 || args.Length % 2 != 0) return false;

            for (int i = 0; i < args.Length; i += 2)
                if (!isKey(args[i])) return false;

            return true;
        }

        public static string JoinValue(string[] args) =>
            args == null || args.Length < 2 ? string.Empty : string.Join(string.Empty, args, 1, args.Length - 1);
    }
}
