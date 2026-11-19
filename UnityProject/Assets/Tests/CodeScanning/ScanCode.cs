using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEngine;


namespace Tests
{
	[Category("Security")]
	public class ScanCode
	{
		[Test, Order(0), Timeout(694267)]
		public void BuildTest()
		{
			BuildTarget target;
			string platform;
			switch (Application.platform)
			{
				case RuntimePlatform.WindowsEditor:
					target = BuildTarget.StandaloneWindows64;
					platform = "Windows";
					break;
				case RuntimePlatform.LinuxEditor:
					target = BuildTarget.StandaloneLinux64;
					platform = "Linux";
					break;
				case RuntimePlatform.OSXEditor:
					target = BuildTarget.StandaloneOSX;
					platform = "Mac";
					break;
				default:
					throw new PlatformNotSupportedException($"CodeScan does not support {Application.platform}.");
			}

			var timer = Stopwatch.StartNew();
			var projectPath = Directory.GetParent(Application.dataPath).FullName;
			// Compile real player code (without UNITY_EDITOR), but skip asset processing,
			// map compression, shaders, and player packaging: CodeScan only reads DLLs.
			// Keep compiler output in Library so Unity can reuse its incremental cache.
			var compilationPath = Path.Combine(projectPath, "Library", "CodeScan", "PlayerAssemblies", platform);
			Directory.CreateDirectory(compilationPath);
			var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
			{
				target = target,
				group = BuildTargetGroup.Standalone,
				subtarget = (int)StandaloneBuildSubtarget.Player,
				options = ScriptCompilationOptions.None
			}, compilationPath);
			result.typeDB?.Dispose();
			Assert.That(result.assemblies, Is.Not.Null.And.Not.Empty, "Player script compilation failed.");

			var managedPath = Path.Combine(projectPath, "Build", platform, platform == "Mac" ? "Mac.app/Contents/Resources/Data/Managed" : "Unitystation_Data/Managed");
			Directory.CreateDirectory(managedPath);

			// Never scan stale assemblies removed or renamed since the previous run.
			foreach (var file in Directory.GetFiles(managedPath, "*.dll"))
			{
				File.Delete(file);
			}

			// Compiler system references contain metadata-only DLLs, including facades
			// such as Microsoft.Win32.Primitives. These are not player code. Use Mono's
			// runtime implementations and omit reference-only facades from the scan input.
			var systemDirectories = CompilationPipeline.GetSystemAssemblyDirectories(
				PlayerSettings.GetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone))
				.Select(Path.GetFullPath).ToArray();
			var runtimePlatform = platform == "Windows" ? "win32" : platform == "Mac" ? "macos" : "linux";
			var runtimePath = Path.Combine(EditorApplication.applicationContentsPath,
				"MonoBleedingEdge", "lib", "mono", "unityjit-" + runtimePlatform);
			Assert.That(File.Exists(Path.Combine(runtimePath, "mscorlib.dll")), Is.True,
				$"Mono player runtime assemblies were not found at {runtimePath}.");

			// Include precompiled dependencies as well as project assemblies.
			var references = CompilationPipeline.GetAssemblies(AssembliesType.Player).SelectMany(assembly => assembly.compiledAssemblyReferences).Distinct();
			foreach (var reference in references)
			{
				var referencePath = Path.GetFullPath(reference);
				var fileName = Path.GetFileName(reference);
				if (systemDirectories.Any(directory => string.Equals(directory,
					Path.GetDirectoryName(referencePath), StringComparison.OrdinalIgnoreCase)))
				{
					referencePath = Path.Combine(runtimePath, fileName);
					if (fileName == "netstandard.dll")
					{
						referencePath = Path.Combine(runtimePath, "Facades", fileName);
					}
					if (File.Exists(referencePath) == false) continue;
				}
				File.Copy(referencePath, Path.Combine(managedPath, fileName), overwrite: true);
			}
			foreach (var assembly in result.assemblies)
			{
				var assemblyPath = Path.Combine(compilationPath, assembly);
				File.Copy(assemblyPath, Path.Combine(managedPath, Path.GetFileName(assembly)), overwrite: true);
			}
			UnityEngine.Debug.Log($"Prepared {result.assemblies.Count} player assemblies for CodeScan in {timer.Elapsed.TotalSeconds:F1}s.");
		}

		[Test, Order(1)]
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
