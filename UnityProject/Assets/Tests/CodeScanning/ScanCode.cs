using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Tests
{
	public class ScanCode
	{
		[Test]
		public void BuildTest()
		{
			string[] levels = new string[] { "Assets/EmptyScene.unity" };

			string pathBuilt = Application.dataPath;
			pathBuilt = pathBuilt.Replace("/Assets", "");

			pathBuilt = Path.Combine(pathBuilt, "Build");

			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(pathBuilt, "Windows", "Unitystation.exe"),
					BuildTarget.StandaloneWindows64, BuildOptions.None);
			}
			else if (Application.platform == RuntimePlatform.LinuxEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(pathBuilt, "Linux", "Unitystation.x86_64"),
					BuildTarget.StandaloneLinux64, BuildOptions.None);
			}
			else if (Application.platform == RuntimePlatform.OSXEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(pathBuilt, "Mac", "Unitystation.x86_64"),
					BuildTarget.StandaloneOSX, BuildOptions.None);
			}
		}


		[Test]
		public void ScanCodeReport()
		{
			string BuildPath = Application.dataPath;
			BuildPath = BuildPath.Replace("/Assets", "");

			var ExecutablePath = BuildPath;

			BuildPath = Path.Combine(BuildPath, "Build");

			var report = new TestReport();


			ExecutablePath =
				ExecutablePath.Replace("UnityProject", @"Tools/CodeScanning/CodeScan/CodeScan/bin/Debug/net7.0");


			var ExtractionPath = ExecutablePath;
			var FolderZip = ExecutablePath;
			var FolderError = "";
			FolderError = ExecutablePath;
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"win-x64.zip");
				FolderError = Path.Combine(FolderError, @"win-x64");
				ExecutablePath += @"/win-x64/CodeScan.exe";
				BuildPath += @"/Windows";
			}
			else if (Application.platform == RuntimePlatform.LinuxEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"linux-x64.zip");
				FolderError = Path.Combine(FolderError, @"linux-x64");
				ExecutablePath += @"/linux-x64/CodeScan";
				BuildPath += @"/Linux";
			}
			else if (Application.platform == RuntimePlatform.OSXEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"osx-x64.zip");
				FolderError = Path.Combine(FolderError, @"osx-x64");
				ExecutablePath += @"/osx-x64/CodeScan";
				BuildPath += @"/Mac";
			}

			if (File.Exists(ExecutablePath) == false)
			{
				ZipFile.ExtractToDirectory(FolderZip, ExtractionPath);
			}

			if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor)
			{
				string filePath = ExecutablePath; // Replace with the actual file path

				// Use the Process class to run the chmod command
				Process chmodProcess = new Process();
				chmodProcess.StartInfo.FileName = "chmod";
				chmodProcess.StartInfo.Arguments = "+x " + filePath;
				chmodProcess.StartInfo.UseShellExecute = false;
				chmodProcess.StartInfo.CreateNoWindow = true;
				chmodProcess.StartInfo.RedirectStandardOutput = true;
				chmodProcess.StartInfo.RedirectStandardError = true;

				chmodProcess.Start();
				chmodProcess.WaitForExit();
				if (chmodProcess.ExitCode == 0)
				{
					UnityEngine.Debug.Log("File marked as executable.");
				}
				else
				{
					UnityEngine.Debug.Log("Failed to mark the file as executable. Exit code: " + chmodProcess.ExitCode);
					UnityEngine.Debug.Log(chmodProcess.StandardError.ReadToEnd());
				}

			}


			// Create a new process
			Process process = new Process();
			process.StartInfo.FileName = ExecutablePath;

			// Redirect standard output and error to Unity's console
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.Arguments = $">\"{BuildPath}\"";

			// Handle the OutputDataReceived event
			//process.OutputDataReceived += (sender, e) => Debug.Log(e.Data);
			//process.ErrorDataReceived += (sender, e) => Debug.LogError(e.Data);

			// Start the process
			process.Start();

			// Begin asynchronous reading of the output stream
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			// Wait for the process to finish
			process.WaitForExit();

			// Cleanup resources
			process.Close();
			process.Dispose();


			var Errors = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(
				FolderError + "/errors.json"));

			foreach (var Error in Errors)
			{
				report.Fail().AppendLine(Error).Log();
			}

			var directory = new DirectoryInfo(BuildPath);
			if (directory.Exists)
			{
				directory.Delete(true);
			}

			var extractionDirectory = new DirectoryInfo(ExtractionPath);
			foreach (var directories in extractionDirectory.GetDirectories())
			{
				if (directories.Name == "MissingAssemblies")
				{
					directories.Delete();
				}
				if (directories.Name == "linux-x64")
				{
					directories.Delete();
				}
				if (directories.Name == "osx-x64")
				{
					directories.Delete();
				}
				if (directories.Name == "win-x64")
				{
					directories.Delete();
				}
			}

			report.Log().AssertPassed();
		}
	}
}