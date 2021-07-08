#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace IgnoranceTransport
{
    public class IgnoranceToolbox
    {
#pragma warning disable IDE0051
        [MenuItem("Ignorance/RTFM/Github Repository")]
        private static void LaunchGithubRepo()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/Ignorance");
        }

        [MenuItem("Ignorance/RTFM/Github Issue Tracker")]
        private static void LaunchGithubIssueTracker()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/Ignorance/issues");
        }

        [MenuItem("Ignorance/RTFM/ENet-CSharp Fork")]
        private static void LaunchENetCSharpForkRepo()
        {
            UnityEngine.Application.OpenURL("https://github.com/SoftwareGuy/ENet-CSharp");
        }

        [MenuItem("Ignorance/Debug/Reveal ENet Native Library Name")]
        public static void RevealEnetLibraryName()
        {
            EditorUtility.DisplayDialog("Enet Library Name", $"Use this for debugging.\nYour platform expects the native Enet library to be called: {ENet.Native.nativeLibraryName}", "Got it");
        }
#pragma warning restore
    }
}
#endif
