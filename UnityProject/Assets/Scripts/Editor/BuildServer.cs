using UnityEditor;
using System.Linq;
using System;

static class BuildScript
{
	private static void PerformServerBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Server/Unitystation-Server";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.None;
		BuildPreferences.SetRelease(true);
        BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformWindowsBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Windows/Unitystation.exe";
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformOSXBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/OSX/Unitystation.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSX;
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformLinuxBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Linux/Unitystation";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformOSXDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Builds/OSX/Unitystation.app";
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;
		buildPlayerOptions.target = BuildTarget.StandaloneOSX;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformLinuxDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Builds/Linux/Unitystation";
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
	private static void PerformWindowsDebugBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		buildPlayerOptions.locationPathName = "../Builds/Windows/Unitystation.exe";
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	// Command Line Arg Build Methods
	// ===============================
	static string GetArgument (string name)
	{
		string[] args = Environment.GetCommandLineArgs ();
		for (int i = 0; i < args.Length; i++) {
			if (args [i].Contains (name)) {
				return args [i + 1];
			}
		}
		return null;
	}

	static string[] GetEnabledScenes ()
	{
		return (
		    from scene in EditorBuildSettings.scenes
		 	where scene.enabled
		 	where !string.IsNullOrEmpty(scene.path)
		 	select scene.path
		).ToArray ();
	}

	static BuildTarget GetBuildTarget ()
	{
		string buildTargetName = GetArgument ("customBuildTarget");
		Console.WriteLine (":: Received customBuildTarget " + buildTargetName);

		if (buildTargetName.ToLower () == "android") {
			#if !UNITY_5_6_OR_NEWER
			// https://issuetracker.unity3d.com/issues/buildoptions-dot-acceptexternalmodificationstoplayer-causes-unityexception-unknown-project-type-0
			// Fixed in Unity 5.6.0
			// side effect to fix android build system:
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Internal;
			#endif
		}

		return ToEnum<BuildTarget> (buildTargetName, BuildTarget.NoTarget);
	}

	static string GetBuildPath ()
	{
		string buildPath = GetArgument ("customBuildPath");
		Console.WriteLine (":: Received customBuildPath " + buildPath);
		if (buildPath == "") {
			throw new Exception ("customBuildPath argument is missing");
		}
		return buildPath;
	}

	static string GetBuildName ()
	{
		string buildName = GetArgument ("customBuildName");
		Console.WriteLine (":: Received customBuildName " + buildName);
		if (buildName == "") {
			throw new Exception ("customBuildName argument is missing");
		}
		return buildName;
	}

	static string GetFixedBuildPath (BuildTarget buildTarget, string buildPath, string buildName) {
		if (buildTarget.ToString().ToLower().Contains("windows")) {
			buildName = buildName + ".exe";
		} else if (buildTarget.ToString().ToLower().Contains("webgl")) {
			// webgl produces a folder with index.html inside, there is no executable name for this buildTarget
			buildName = "";
		}
		return "../" + buildPath + buildName;
	}

	static BuildOptions GetBuildOptions ()
	{
		string buildOptions = GetArgument ("customBuildOptions");
		return buildOptions == "AcceptExternalModificationsToPlayer" ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None;
	}

	// https://stackoverflow.com/questions/1082532/how-to-tryparse-for-enum-value
	static TEnum ToEnum<TEnum> (this string strEnumValue, TEnum defaultValue)
	{
		if (!Enum.IsDefined (typeof(TEnum), strEnumValue)) {
			return defaultValue;
		}

		return (TEnum)Enum.Parse (typeof(TEnum), strEnumValue);
	}

	static string getEnv (string key, bool secret = false, bool verbose = true)
	{
		var env_var = Environment.GetEnvironmentVariable (key);
		if (verbose) {
			if (env_var != null) {
				if (secret) {
					Console.WriteLine (":: env['" + key + "'] set");
				} else {
					Console.WriteLine (":: env['" + key + "'] set to '" + env_var + "'");
				}
			} else {
				Console.WriteLine (":: env['" + key + "'] is null");
			}
		}
		return env_var;
	}

	static void PerformBuild ()
	{
		Console.WriteLine (":: Performing build");
		//PlayerSettings.keystorePass = getEnv ("KEYSTORE_PASS", true);
		//PlayerSettings.keyaliasPass = getEnv ("KEY_ALIAS_PASS", true);
		//EditorSetup.AndroidSdkRoot = getEnv ("ANDROID_SDK_HOME");
		//EditorSetup.JdkRoot = getEnv ("JAVA_HOME");
		//EditorSetup.AndroidNdkRoot = getEnv ("ANDROID_NDK_HOME");
		var buildScenes = new[] {"Assets/scenes/Lobby.unity", "Assets/scenes/OutpostStation.unity"};
		var buildTarget = GetBuildTarget();
		var buildPath = GetBuildPath();
		var buildName = GetBuildName();
		var fixedBuildPath = GetFixedBuildPath (buildTarget, buildPath, buildName);

		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = buildScenes;
		buildPlayerOptions.locationPathName = fixedBuildPath;
		buildPlayerOptions.target = buildTarget;
		buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging; //| BuildOptions.CompressWithLz4HC;
		// BuildPreferences.SetRelease(true);

		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}
}