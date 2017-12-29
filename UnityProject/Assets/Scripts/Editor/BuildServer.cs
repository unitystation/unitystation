using UnityEditor;

public class BuildServer
{
	private static void PerformBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "Builds/Server/Linux/Unitystation-Server";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
}