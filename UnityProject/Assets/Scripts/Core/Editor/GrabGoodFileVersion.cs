using System;
using System.Diagnostics;
using DatabaseAPI;
using Logs;
using Newtonsoft.Json;
using SecureStuff;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class GrabGoodFileVersion : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 9; }
	}

	public void OnPreprocessBuild(BuildTarget target, string path)
	{
		var Gamedata = AssetDatabase.LoadAssetAtPath<GameObject>(
			"Assets/Prefabs/SceneConstruction/NestedManagers/GameData.prefab");
		if (Gamedata.GetComponent<GameData>().DevBuild)
		{
			return;
		}

		try
		{
			// Get the latest good-file version tag
			string latestTag = GetLatestGoodFileVersion();

			var BuildInfo = JsonConvert.DeserializeObject<BuildInfo>(AccessFile.Load("buildinfo.json"));
			BuildInfo.GoodFileVersion = latestTag.Replace("good-file-", "");
			AccessFile.Save("buildinfo.json", JsonConvert.SerializeObject(BuildInfo));
		}
		catch (Exception ex)
		{
			Loggy.Warning("Not able to set good file version " + ex.ToString());
		}
	}

	private string GetLatestGoodFileVersion()
	{
		try
		{
			// Set up the Git process to get tags
			Process gitProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "git",
					Arguments = "tag --list good-file-* --sort=-v:refname",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = Environment.CurrentDirectory // Explicitly set the working directory
				}
			};

			UnityEngine.Debug.Log("[GrabGoodFileVersion] Running Git command to fetch tags...");
			UnityEngine.Debug.Log($"[GrabGoodFileVersion] Command: git {gitProcess.StartInfo.Arguments}");
			UnityEngine.Debug.Log(
				$"[GrabGoodFileVersion] Current Working Directory: {gitProcess.StartInfo.WorkingDirectory}");

			// Start the process and capture the output
			gitProcess.Start();
			string output = gitProcess.StandardOutput.ReadToEnd().Trim();
			string error = gitProcess.StandardError.ReadToEnd();
			gitProcess.WaitForExit();

			UnityEngine.Debug.Log($"[GrabGoodFileVersion] Git process exited with code {gitProcess.ExitCode}.");
			UnityEngine.Debug.Log($"[GrabGoodFileVersion] Standard Output:\n{output}");
			UnityEngine.Debug.Log($"[GrabGoodFileVersion] Standard Error:\n{error}");

			if (gitProcess.ExitCode != 0)
			{
				UnityEngine.Debug.LogError(
					$"[GrabGoodFileVersion] Git process failed with exit code {gitProcess.ExitCode}.");
				return null;
			}

			// Split the output into lines and take the first line as the latest tag
			string[] tags = output.Split('\n');
			return tags.Length > 0 ? tags[0] : null;
		}
		catch (System.Exception ex)
		{
			UnityEngine.Debug.LogError($"An error occurred while retrieving the Git tag: {ex.Message}");
			return null;
		}
	}
}