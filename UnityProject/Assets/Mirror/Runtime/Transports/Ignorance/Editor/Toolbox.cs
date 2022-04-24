// Ignorance 1.4.x LTS (Long Term Support)
// https://github.com/SoftwareGuy/Ignorance
// -----------------
// Copyright (c) 2019 - 2021 Matt Coburn (SoftwareGuy/Coburn64)
// Ignorance is licensed under the MIT license. Refer
// to the LICENSE file for more information.
#if UNITY_EDITOR
using UnityEditor;

namespace IgnoranceTransport
{
    public class Toolbox
    {
#pragma warning disable IDE0051
        [MenuItem("Ignorance/Debug/Native Library Name")]
        public static void RevealEnetLibraryName()
        {
            EditorUtility.DisplayDialog("ENet Library Name", $"Your platform expects the native library to be called: {ENet.Native.nativeLibraryName}.\n\n" +
                $"This info is very useful when trying to diagnose issues with DLL loading.", "Got it");
        }

        [MenuItem("Ignorance/RTFM/Github Repo")]
        private static void LaunchGithubRepo()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/Ignorance");
        }

        [MenuItem("Ignorance/RTFM/Issue Tracker")]
        private static void LaunchGithubIssueTracker()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/Ignorance/issues");
        }

        [MenuItem("Ignorance/RTFM/ENet-C# Repo")]
        private static void LaunchENetCSharpForkRepo()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/ENet-CSharp");
        }

#pragma warning restore
    }
}
#endif
