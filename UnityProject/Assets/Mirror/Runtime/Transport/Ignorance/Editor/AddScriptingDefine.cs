// Ignorance 1.4.x
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance Transport is licensed under the MIT license. Refer
// to the LICENSE file for more information.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace IgnoranceTransport
{
    /// <summary>
    /// Adds the given define symbols to PlayerSettings define symbols.
    /// Just add your own define symbols to the Symbols property at the below.
    /// </summary>
    [InitializeOnLoad]
    public class AddIgnoranceDefine : Editor
    {
        private static bool debugThisShit = false;
        private static string existingDefines = string.Empty;

        /// <summary>
        /// Symbols that will be added to the editor
        /// </summary>
        public static readonly string[] Symbols = new string[] {
            "IGNORANCE", // Ignorance exists
            "IGNORANCE_1", // Major version
            "IGNORANCE_1_4" // Major and minor version
        };

        /// <summary>
        /// Do not remove these symbols
        /// </summary>
        public static readonly string[] DoNotRemoveTheseSymbols = new string[]
        {
            "IGNORANCE",
            "IGNORANCE_1",
            "IGNORANCE_1_4",
            "IGNORANCE_MIRROR_POLLING"
        };

        /// <summary>
        /// Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        static AddIgnoranceDefine()
        {
            // Get the current scripting defines
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (existingDefines == definesString)
            {
                // 1.2.6: There is no need to apply the changes, return.
                return;
            }

            // Convert the string to a list
            List<string> allDefines = definesString.Split(';').ToList();
            // Remove any old version defines from previous installs
            allDefines.RemoveAll(IsSafeToRemove);
            // x => x.StartsWith("IGNORANCE") && !DoesSymbolExistInBlacklist(x));
            // Add any symbols that weren't already in the list
            allDefines.AddRange(Symbols.Except(allDefines));

            string newDefines = string.Join(";", allDefines.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                newDefines
            );

            existingDefines = newDefines;
        }

        // 1.2.4: Workaround to stop things from eating custom IGNORANCE_ symbols
        static bool DoesSymbolExistInBlacklist(string symbol)
        {
            foreach(string s in DoNotRemoveTheseSymbols)
            {
                if(debugThisShit)
                    UnityEngine.Debug.Log($"{s.Trim()} matches blacklist");

                if (s == symbol.Trim()) return true;
            }

            return false;
        }

        static bool IsSafeToRemove (string input)
        {
            if (input.StartsWith("IGNORANCE") && !DoesSymbolExistInBlacklist(input))
            {
                if (debugThisShit)
                    UnityEngine.Debug.Log($"{input.Trim()} is safe to remove");

                return true;
            }

            return false;
        }
    }
}
#endif
