using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;


namespace Tests
{
	[Category("Security")]
	public class ScanCode
	{
		[Test, Timeout(694267)]
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
			var projectPath = Directory.GetParent(Application.dataPath).FullName;
			var BuildPath = Path.Combine(projectPath, "Build");
			string runtimeIdentifier;
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				runtimeIdentifier = "win-x64";
				BuildPath = Path.Combine(BuildPath, "Windows");
			}
			else if (Application.platform == RuntimePlatform.LinuxEditor)
			{
				runtimeIdentifier = "linux-x64";
				BuildPath = Path.Combine(BuildPath, "Linux");
			}
			else if (Application.platform == RuntimePlatform.OSXEditor)
			{
				runtimeIdentifier = "osx-x64";
				BuildPath = Path.Combine(BuildPath, "Mac");
			}
			else
			{
				throw new PlatformNotSupportedException($"CodeScan does not support {Application.platform}.");
			}

			var ExtractionPath = Path.Combine(projectPath, "Library", "CodeScan");
			var ExecutablePath = CodeScanRelease.EnsureAvailable(ExtractionPath, runtimeIdentifier);
			var FolderError = Path.GetDirectoryName(ExecutablePath);
			var report = new TestReport();
			var errorsPath = Path.Combine(FolderError, "errors.json");
			var generatedWhitelistPath = Path.Combine(FolderError, "CodeScanList.corrected.json");
			var extractedWhitelistPath = Path.Combine(ExtractionPath, "CodeScanList.corrected.json");

			// A cached executable must produce a fresh report for every scan.
			File.Delete(errorsPath);
			File.Delete(generatedWhitelistPath);
			File.Delete(extractedWhitelistPath);

			if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.OSXEditor)
			{
				using (var chmodProcess = new Process())
				{
					chmodProcess.StartInfo.FileName = "chmod";
					chmodProcess.StartInfo.Arguments = $"+x \"{ExecutablePath}\"";
					chmodProcess.StartInfo.UseShellExecute = false;
					chmodProcess.StartInfo.CreateNoWindow = true;
					chmodProcess.Start();
					chmodProcess.WaitForExit();
					Assert.That(chmodProcess.ExitCode, Is.Zero, "Failed to mark CodeScan as executable.");
				}
			}

			using (var process = new Process())
			{
				process.StartInfo.FileName = ExecutablePath;
				process.StartInfo.WorkingDirectory = FolderError;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.Arguments = $">\"{BuildPath}\"";
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				process.WaitForExit();
				Assert.That(process.ExitCode, Is.Zero, "CodeScan exited unexpectedly.");
			}

			if (File.Exists(generatedWhitelistPath))
			{
				File.Copy(generatedWhitelistPath, extractedWhitelistPath, overwrite: true);
				report.Fail()
					.AppendLine($"CodeScan whitelist suggestions were saved to: {extractedWhitelistPath}")
					.Log();
			}

			Assert.That(File.Exists(errorsPath), Is.True, "CodeScan did not produce an errors.json report.");
			var Errors = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(errorsPath));
			Assert.That(Errors, Is.Not.Null, "CodeScan produced an invalid errors.json report.");

			foreach (var Error in Errors)
			{
				report.Fail().AppendLine(Error).Log();
			}

			var directory = new DirectoryInfo(BuildPath);
			if (directory.Exists)
			{
				directory.Delete(true);
			}

			var missingAssemblies = Path.Combine(Path.GetDirectoryName(FolderError), "MissingAssemblies");
			if (Directory.Exists(missingAssemblies)) Directory.Delete(missingAssemblies, true);

			report.Log().AssertPassed();
		}
	}
}
