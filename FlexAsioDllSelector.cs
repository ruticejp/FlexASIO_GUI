using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexASIOGUI
{
    /// <summary>
    /// Helper for choosing the best FlexASIO.dll candidate from a set of discovered paths.
    /// </summary>
    public static class FlexAsioDllSelector
    {
        /// <summary>
        /// Picks the best FlexASIO.dll candidate, preferring x64 builds and higher file versions.
        /// </summary>
        /// <param name="candidates">A list of candidate paths.</param>
        /// <param name="getVersion">A function that returns the file version for a given path (or null if not available).</param>
        /// <param name="is64Bit">A function that returns true if the path points to a 64-bit binary.</param>
        /// <returns>The best candidate path, or null if the input list is empty.</returns>
        public static string ChooseBestDll(IEnumerable<string> candidates, Func<string, Version> getVersion, Func<string, bool> is64Bit)
        {
            if (candidates == null)
                return null;

            var scored = candidates
                .Select(p => (path: p, version: getVersion(p), is64: is64Bit(p)))
                .Where(x => x.version != null)
                .OrderByDescending(x => x.is64)
                .ThenByDescending(x => x.version)
                .ToArray();

            if (scored.Length > 0)
                return scored[0].path;

            return candidates.FirstOrDefault();
        }
    }
}
