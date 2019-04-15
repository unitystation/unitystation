using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PerformanceBenchmarkReport : MonoBehaviour
{
	const string ReporterFolderName           = @"UnityPerformanceBenchmarkReporter";
	const string ReporterDllName              = @"UnityPerformanceBenchmarkReporter.dll";
	const string PerformanceResultFolderPath  = @"Assets\StreamingAssets";
	const string PerformanceResultName        = @"TestResults.xml";
	const string PerformanceResultsFolderName = @"TestResults";
	const string BaselineResultName           = @"BaselineResult.xml";
	const string BenchmarkReportFolderName    = @"UnityPerformanceBenchmark";

	static readonly string PerformanceResultLocation        = Path.Combine(Environment.CurrentDirectory, PerformanceResultFolderPath, PerformanceResultName);
	static readonly string ReporterFolderLocation           = Path.Combine(Environment.CurrentDirectory, ReporterFolderName);
	static readonly string PerformanceResultsFolderLocation = Path.Combine(ReporterFolderLocation, PerformanceResultsFolderName);
	static readonly string BaselineResultLocation           = Path.Combine(ReporterFolderLocation, BaselineResultName);
	static readonly string BenchmarkReportFolderLocation    = Path.Combine(ReporterFolderLocation, BenchmarkReportFolderName);

	[MenuItem("Tools/Performance Test Results/Open Benchmark Report")]
	static void OpenBenchmarkReport()
	{
		var dir = new DirectoryInfo(BenchmarkReportFolderLocation);
		var file = (from f in dir.GetFiles()
					orderby f.LastWriteTime descending
					select f).First();

		System.Diagnostics.Process.Start(file.FullName);
	}

	[MenuItem("Tools/Performance Test Results/Add Performance Result To Comparison")]
	static void AddPerformanceResult()
	{
		if (!Directory.Exists(PerformanceResultsFolderLocation)) Directory.CreateDirectory(PerformanceResultsFolderLocation);
		var file = new FileInfo(PerformanceResultLocation);

		var OutputFile = Path.Combine(PerformanceResultsFolderLocation,
			Path.GetFileNameWithoutExtension(file.Name) +
			file.LastWriteTime.ToUniversalTime().ToString("yyyy-MM-dd_hh-mm-ss") +
			file.Extension);

		if (File.Exists(OutputFile)) return;
		File.Copy(file.FullName, OutputFile);

		RegenerateReport();
	}

	[MenuItem("Tools/Performance Test Results/Delete All Performance Results From Comparison")]
	static void DeletePerformanceResults()
	{
		var dir = new DirectoryInfo(PerformanceResultsFolderLocation);
		foreach (var file in dir.GetFiles())
		{
			file.Delete();
		}
		RegenerateReport();
	}

	[MenuItem("Tools/Performance Test Results/Use Performance Result As Baseline")]
	static void SetBaselineResult()
	{
		if (File.Exists(BaselineResultLocation)) File.Delete(BaselineResultLocation);
		File.Copy(PerformanceResultLocation, BaselineResultLocation);

		RegenerateReport();
	}

	[MenuItem("Tools/Performance Test Results/Remove Baseline Result From Comparison")]
	static void DeleteBaselineResult()
	{
		if (File.Exists(BaselineResultLocation)) File.Delete(BaselineResultLocation);
		RegenerateReport();
	}

	[MenuItem("Tools/Performance Test Results/Regenerate Benchmark Report")]
	static void RegenerateReport()
	{
		var cmd = new System.Diagnostics.Process();
		cmd.StartInfo.FileName = "cmd.exe";
		cmd.StartInfo.RedirectStandardInput = true;
		cmd.StartInfo.RedirectStandardOutput = true;
		cmd.StartInfo.CreateNoWindow = true;
		cmd.StartInfo.UseShellExecute = false;
		cmd.Start();

		cmd.StandardInput.WriteLine($"cd {Path.Combine(Environment.CurrentDirectory, ReporterFolderName)}");
		cmd.StandardInput.WriteLine(
			$"dotnet {ReporterDllName} " +
			$"--results=\"{PerformanceResultsFolderName}\" " +
			(File.Exists(BaselineResultLocation) ?
				$"--baseline=\"{BaselineResultName}\" " :
				" "));
		cmd.StandardInput.Flush();
		cmd.StandardInput.Close();
		cmd.WaitForExit();
		Debug.Log(cmd.StandardOutput.ReadToEnd());
	}

	[MenuItem("Tools/Performance Test Results/Open Reporter Location")]
	static void OpenReporterLocation()
	{
		System.Diagnostics.Process.Start(ReporterFolderLocation);
	}
}