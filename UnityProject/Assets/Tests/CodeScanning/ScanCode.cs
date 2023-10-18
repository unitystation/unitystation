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
			return;
			string[] levels = new string[] {};


			string path = Application.dataPath;
			path = path.Replace("/Assets", "");

			path = Path.Combine(path, "Build");

			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(path, "Windows", "Unitystation.exe"), BuildTarget.StandaloneWindows64, BuildOptions.None);
			}
			else if (Application.platform == RuntimePlatform.LinuxEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(path, "Linux", "Unitystation.x86_64") , BuildTarget.StandaloneLinux64, BuildOptions.None);
			}
			else if (Application.platform == RuntimePlatform.OSXEditor)
			{
				BuildPipeline.BuildPlayer(levels, Path.Combine(path, "Mac", "Unitystation.x86_64") , BuildTarget.StandaloneOSX, BuildOptions.None);
			}

		}

		[Test]
		public void ScanCodeReport()
		{

			string path = Application.dataPath;
			path = path.Replace("/Assets", "");

			var ExecutablePath = path;

			path = Path.Combine(path, "Build");

			var report = new TestReport();


			ExecutablePath = ExecutablePath.Replace("UnityProject", @"Tools\CodeScanning\CodeScan\CodeScan\bin\Debug\net7.0");


			var ExtractionPath = ExecutablePath;
			var FolderZip = ExecutablePath;
			var FolderError = "";
			FolderError = ExecutablePath;
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"win-x64.zip");
				FolderError = Path.Combine(FolderError, @"win-x64");
				ExecutablePath += @"\win-x64\CodeScan.exe";
				path += @"\Windows";
			}
			else if (Application.platform == RuntimePlatform.LinuxEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"linux-x64.zip");
				FolderError = Path.Combine(FolderError, @"linux-x64");
				ExecutablePath += @"\linux-x64\CodeScan";
				path += @"\Linux";
			}
			else if (Application.platform == RuntimePlatform.OSXEditor)
			{
				FolderZip = Path.Combine(FolderZip, @"osx-x64.zip");
				FolderError = Path.Combine(FolderError, @"osx-x64");
				ExecutablePath += @"\osx-x64\CodeScan";
				path += @"\Mac";
			}

			if (Directory.Exists(FolderError) == false)
			{
				ZipFile.ExtractToDirectory(FolderZip , ExtractionPath);
			}


			// Create a new process
			Process process = new Process();
			process.StartInfo.FileName = ExecutablePath;

			// Redirect standard output and error to Unity's console
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.Arguments = $">\"{path}\"";

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


			report.Log().AssertPassed();

		}
	}
}