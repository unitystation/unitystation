using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;

static class BuildScript
{
	[Obsolete]
	private static void PerformServerBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/StartUp.unity","Assets/scenes/Lobby.unity",
			"Assets/scenes/BoxStationV1.unity", "Assets/scenes/OutpostStation.unity",
			"Assets/scenes/PogStation.unity", "Assets/scenes/AsteroidStation.unity"
		};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Server/Unitystation-Server";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.Development;
		BuildPreferences.SetRelease(true);
        BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	//IMPORTANT: ALWAYS DO WINDOWS BUILD FIRST IN YOUR BUILD CYCLE:
	[Obsolete]
	private static void PerformWindowsBuild()
	{
		//Always build windows client first so that build info can increment the build number
		var buildInfo = JsonUtility.FromJson<BuildInfo>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "buildinfo.json")));
		BuildInfo buildInfoUpdate = new BuildInfo();
		if (File.Exists(Path.Combine(Application.streamingAssetsPath, "buildinfoupdate.json")))
		{
			buildInfoUpdate = JsonUtility.FromJson<BuildInfo>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "buildinfoupdate.json")));

			//Allows build number ranges to be forced by committing a change to buildinfo.json
			//buildinfoupdate is gitignored so it can be stored on the build server
			if (buildInfoUpdate.BuildNumber < buildInfo.BuildNumber)
			{
				buildInfoUpdate.BuildNumber = buildInfo.BuildNumber;
			}
		}
		else
		{
			buildInfoUpdate.BuildNumber = buildInfo.BuildNumber;
		}

		buildInfoUpdate.BuildNumber++;
		buildInfo.BuildNumber = buildInfoUpdate.BuildNumber;
		File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "buildinfo.json"), JsonUtility.ToJson(buildInfo));
		File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "buildinfoupdate.json"), JsonUtility.ToJson(buildInfoUpdate));

		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/StartUp.unity","Assets/scenes/Lobby.unity",
			"Assets/scenes/BoxStationV1.unity", "Assets/scenes/OutpostStation.unity",
			"Assets/scenes/PogStation.unity", "Assets/scenes/AsteroidStation.unity"
		};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Windows/Unitystation.exe";
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	[Obsolete]
	private static void PerformOSXBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/StartUp.unity","Assets/scenes/Lobby.unity",
			"Assets/scenes/BoxStationV1.unity", "Assets/scenes/OutpostStation.unity",
			"Assets/scenes/PogStation.unity", "Assets/scenes/AsteroidStation.unity"
		};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/OSX/Unitystation.app";
		buildPlayerOptions.target = BuildTarget.StandaloneOSX;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	[Obsolete]
	private static void PerformLinuxBuild()
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = new[] {"Assets/scenes/StartUp.unity","Assets/scenes/Lobby.unity",
			"Assets/scenes/BoxStationV1.unity", "Assets/scenes/OutpostStation.unity",
			"Assets/scenes/PogStation.unity", "Assets/scenes/AsteroidStation.unity"
		};
		buildPlayerOptions.locationPathName = "../Tools/ContentBuilder/content/Linux/Unitystation";
		buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
		buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;
		BuildPreferences.SetRelease(true);
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	// ##################################
	// #   Command Line Build Methods   #
	// ##################################

	private static string EOL = Environment.NewLine;

	private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
	{
		providedArguments = new Dictionary<string, string>();
		string[] args = Environment.GetCommandLineArgs();

		Console.WriteLine(
			$"{EOL}" +
			$"###########################{EOL}" +
			$"#    Parsing settings     #{EOL}" +
			$"###########################{EOL}" +
			$"{EOL}"
		);

		// Extract flags with optional values
		for (int current = 0, next = 1; current < args.Length; current++, next++) {
			// Parse flag
			bool isFlag = args[current].StartsWith("-");
			if (!isFlag) continue;
			string flag = args[current].TrimStart('-');

			// Parse optional value
			bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
			string value = flagHasValue ? args[next].TrimStart('-') : "";

			// Assign
			Console.WriteLine($"Found flag \"{flag}\" with value \"{value}\".");
			providedArguments.Add(flag, value);
		}
	}

	private static Dictionary<string, string> GetValidatedOptions()
	{
		ParseCommandLineArguments(out var validatedOptions);

		if (!validatedOptions.TryGetValue("projectPath", out var projectPath)) {
			Console.WriteLine("Missing argument -projectPath");
			EditorApplication.Exit(110);
		}

		if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget)) {
			Console.WriteLine("Missing argument -buildTarget");
			EditorApplication.Exit(120);
		}

		if (!Enum.IsDefined(typeof(BuildTarget), buildTarget)) {
			EditorApplication.Exit(121);
		}

		if (!validatedOptions.TryGetValue("customBuildPath", out var customBuildPath)) {
			Console.WriteLine("Missing argument -customBuildPath");
			EditorApplication.Exit(130);
		}

		string defaultCustomBuildName = "TestBuild";
		if (!validatedOptions.TryGetValue("customBuildName", out var customBuildName)) {
			Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
			validatedOptions.Add("customBuildName", defaultCustomBuildName);
		}
		else if (customBuildName == "") {
			Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
			validatedOptions.Add("customBuildName", defaultCustomBuildName);
		}

		return validatedOptions;
	}

	private static string GetFileExtension(string buildTarget)
	{
		var target = buildTarget.ToLower();

		if (target.Contains("windows")) return ".exe";
		if (target.Contains("osx")) return ".app";

		return null;
	}

	public static void BuildProject()
	{
		// Gather values from args
		var options = GetValidatedOptions();

		var buildTarget = options["buildTarget"];
		var buildPath = options["customBuildPath"];
		// TODO: fix Unity Builder Action to use customBuildName
		// var buildName = options["customBuildName"];

		// Gather values from project
		var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
		// var locationPathName = $"{buildPath}/{buildName}{GetFileExtension(buildTarget)}";
		var locationPathName = buildPath;
		var target = (BuildTarget) Enum.Parse(typeof(BuildTarget), buildTarget);

		// Define BuildPlayer Options
		var buildOptions = new BuildPlayerOptions {
			scenes = scenes,
			locationPathName = locationPathName,
			target = target,
			options = BuildOptions.CompressWithLz4HC
		};

		if (target == BuildTarget.StandaloneLinux64)
		{
			buildOptions.options |= BuildOptions.Development;
		}

		ReportOptions(buildOptions);

		// Perform build
		BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);

		// Summary
		BuildSummary summary = buildReport.summary;
		ReportSummary(summary);

		// Result
		BuildResult result = summary.result;
		ExitWithResult(result);
	}

	private static void ReportOptions(BuildPlayerOptions options)
	{
		Console.WriteLine(
			$"{EOL}" +
			$"###########################{EOL}" +
			$"#      Build options      #{EOL}" +
			$"###########################{EOL}" +
			$"{EOL}" +
			$"Scenes: {options.scenes.ToString()}{EOL}" +
			$"Path: {options.locationPathName.ToString()}{EOL}" +
			$"Target: {options.target.ToString()}{EOL}" +
			$"{EOL}"
		);
	}

	private static void ReportSummary(BuildSummary summary)
	{
		Console.WriteLine(
			$"{EOL}" +
			$"###########################{EOL}" +
			$"#      Build results      #{EOL}" +
			$"###########################{EOL}" +
			$"{EOL}" +
			$"Duration: {summary.totalTime.ToString()}{EOL}" +
			$"Warnings: {summary.totalWarnings.ToString()}{EOL}" +
			$"Errors: {summary.totalErrors.ToString()}{EOL}" +
			$"Size: {summary.totalSize.ToString()} bytes{EOL}" +
			$"{EOL}"
		);
	}

	private static void ExitWithResult(BuildResult result)
	{
		if (result == BuildResult.Succeeded) {
			Console.WriteLine("Build succeeded!");
			EditorApplication.Exit(0);
		}

		if (result == BuildResult.Failed) {
			Console.WriteLine("Build failed!");
			EditorApplication.Exit(101);
		}

		if (result == BuildResult.Cancelled) {
			Console.WriteLine("Build cancelled!");
			EditorApplication.Exit(102);
		}

		if (result == BuildResult.Unknown) {
			Console.WriteLine("Build result is unknown!");
			EditorApplication.Exit(103);
		}
	}
}
