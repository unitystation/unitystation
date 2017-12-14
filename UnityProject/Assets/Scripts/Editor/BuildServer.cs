using UnityEditor;

public class BuildServer
{
    private static void PerformBuild()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/Deathmatch.unity"};
        buildPlayerOptions.locationPathName = "Builds/server/server";
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}