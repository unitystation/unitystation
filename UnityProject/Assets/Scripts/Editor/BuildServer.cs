using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildServer
{
    static void PerformBuild()
    {
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/scenes/Lobby.unity", "Assets/scenes/Deathmatch.unity" };
        buildPlayerOptions.locationPathName = "Builds/server/server";
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
