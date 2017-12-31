using UnityEditor;

public static class BuildServer
{
	private static string[] scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};

	private static void PerformOSXBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = scenes;
		buildPlayerOptions.locationPathName = "Builds/Client/OSX/Unitystation-Server.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSXUniversal;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	private static void PerformOSXServerBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = scenes;
		buildPlayerOptions.locationPathName = "Builds/Server/OSX/Unitystation-Server.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSXIntel64;
		buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	private static void PerformBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = scenes;
		buildPlayerOptions.locationPathName = "Builds/Server/Linux/Unitystation-Server";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
}
