using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

static class BuildScript
{
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
		for (int current = 0, next = 1; current < args.Length; current++, next++)
		{
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

		if (!validatedOptions.TryGetValue("projectPath", out _))
		{
			Console.WriteLine("Missing argument -projectPath");
			EditorApplication.Exit(110);
		}

		if (!validatedOptions.TryGetValue("buildTarget", out var buildTarget))
		{
			Console.WriteLine("Missing argument -buildTarget");
			EditorApplication.Exit(120);
		}

		if (!Enum.IsDefined(typeof(BuildTarget), buildTarget))
		{
			Console.WriteLine("Invalid -targetBuild value.");
			EditorApplication.Exit(121);
		}

		if (!validatedOptions.TryGetValue("customBuildPath", out _))
		{
			Console.WriteLine("Missing argument -customBuildPath");
			EditorApplication.Exit(130);
		}

		if (validatedOptions.TryGetValue("devBuild", out var devBuild))
		{
			Console.WriteLine("Found -devBuild argument. This build will be a devBuild and include deep profiling!");
			validatedOptions["devBuild"] = "true";
		}
		else
		{
			validatedOptions.Add("devBuild", "false");
		}

		return validatedOptions;
	}

	public static void BuildProject()
	{
		// Gather values from args
		var options = GetValidatedOptions();

		var buildTarget = options["buildTarget"];
		var buildPath = options["customBuildPath"];
		var devBuild = options["devBuild"];

		// Gather values from project
		var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();

		var locationPathName = buildPath;

		var target = (BuildTarget) Enum.Parse(typeof(BuildTarget), buildTarget);

		// Define BuildPlayer Options
		var buildOptions = new BuildPlayerOptions {
			scenes = scenes,
			locationPathName = locationPathName,
			target = target,
			options = BuildOptions.CompressWithLz4HC
		};

		if (devBuild.Equals("true"))
		{
			buildOptions.options |= BuildOptions.Development;
			buildOptions.options |= BuildOptions.EnableDeepProfilingSupport;
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
		switch (result)
		{
			case BuildResult.Succeeded:
				Console.WriteLine("Build succeeded!");
				EditorApplication.Exit(0);
				break;
			case BuildResult.Failed:
				Console.WriteLine("Build failed!");
				EditorApplication.Exit(101);
				break;
			case BuildResult.Cancelled:
				Console.WriteLine("Build cancelled!");
				EditorApplication.Exit(102);
				break;
			default:
				Console.WriteLine("Build result is unknown!");
				EditorApplication.Exit(103);
				break;
		}
	}
}
