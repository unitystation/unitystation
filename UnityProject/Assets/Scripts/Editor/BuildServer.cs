using UnityEditor;

public class BuildScript
{
	private static void PerformServerBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Server/Unitystation-Server";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.EnableHeadlessMode;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformWindowsBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Windows/Unitystation.exe";
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformOSXBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/OSX/Unitystation.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSX;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformLinuxBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Linux/Unitystation";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformOSXDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Builds/OSX/Unitystation.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSX;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformLinuxDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Builds/Linux/Unitystation";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformWindowsDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostDeathmatch.unity"};
		buildPlayerOptions.locationPathName = "../Builds/Windows/Unitystation.exe";
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
}