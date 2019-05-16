using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BenchmarkReportEditorUI : EditorWindow
{
	const string ReporterFolderName = @"UnityPerformanceBenchmarkReporter";
	const string ReporterDllName = @"UnityPerformanceBenchmarkReporter.dll";
	const string ResultFolderPath = @"Assets\StreamingAssets";
	const string ResultName = @"TestResults.xml";
	const string SelectedResultsFolderName = @"SelectedTestResults";
	const string UnselectedResultsFolderName = @"UnselectedTestResults";
	const string BaselineResultFolderName = @"BaselineTestResult";
	const string BenchmarkReportFolderName = @"UnityPerformanceBenchmark";

	static readonly string ResultLocation = Path.Combine(Environment.CurrentDirectory, ResultFolderPath, ResultName);
	static readonly string ReporterFolderLocation = Path.Combine(Environment.CurrentDirectory, ReporterFolderName);
	static readonly string SelectedResultsFolderLocation = Path.Combine(ReporterFolderLocation, SelectedResultsFolderName);
	static readonly string UnselectedResultsFolderLocation = Path.Combine(ReporterFolderLocation, UnselectedResultsFolderName);
	static readonly string BaselineResultFolderLocation = Path.Combine(ReporterFolderLocation, BaselineResultFolderName);
	static readonly string BenchmarkReportFolderLocation = Path.Combine(ReporterFolderLocation, BenchmarkReportFolderName);

	static DirectoryInfo SelectedResultsFolder => new DirectoryInfo(SelectedResultsFolderLocation);
	static DirectoryInfo UnselectedResultsFolder => new DirectoryInfo(UnselectedResultsFolderLocation);
	static DirectoryInfo BaselineResultFolder => new DirectoryInfo(BaselineResultFolderLocation);
	static DirectoryInfo BenchmarkReportFolder => new DirectoryInfo(BenchmarkReportFolderLocation);

	static FileInfo Baseline => BaselineResultFolder.EnumerateFiles("*.xml").FirstOrDefault();
	static FileInfo Report => (from f in BenchmarkReportFolder.EnumerateFiles("*.html")
							   orderby f.LastWriteTime descending
							   select f).FirstOrDefault();

	[MenuItem("Window/Benchmark Report")] static void ShowWindow() => GetWindow<BenchmarkReportEditorUI>("Benchmark Report");

	void OnEnable()
	{
		CreateFolders();
	}

	void OnInspectorUpdate()
	{
		Repaint();
	}

	void OnGUI()
	{
		Buttons();
		EditorGUILayout.Separator();
		EditorGUILayout.BeginHorizontal();
		UnselectedBenchmarkResults();
		SelectedResults();
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.LabelField("Shift click result to use as baseline");
	}

	void Buttons()
	{
		EditorGUILayout.BeginHorizontal();
		AddResult();
		CreateReport();
		OpenReport();
		OpenReporterLocation();
		EditorGUILayout.EndHorizontal();
	}

	void AddResult()
	{
		var disabled = !File.Exists(ResultLocation);
		if (!disabled)
		{
			var fileName = FileName(new FileInfo(ResultLocation));

			var UnselectedFileLocation = Path.Combine(UnselectedResultsFolderLocation, fileName);
			var SelectedFileLocation   = Path.Combine(SelectedResultsFolderLocation, fileName);
			var BaselineFileLocation   = Path.Combine(BaselineResultFolderLocation, fileName);

			if (File.Exists(UnselectedFileLocation) ||
				File.Exists(SelectedFileLocation  ) ||
				File.Exists(BaselineFileLocation  ))
			{
				disabled = true;
			}
		}


		using (new EditorGUI.DisabledScope(disabled))
			if (GUILayout.Button("Add Result"))
			{
				var file = new FileInfo(ResultLocation);
				var fileName = FileName(file);

				var UnselectedFileLocation = Path.Combine(UnselectedResultsFolderLocation, fileName);
				File.Copy(file.FullName, UnselectedFileLocation);
			}

		string FileName(FileInfo sourceFile)
		{
			return Path.GetFileNameWithoutExtension(sourceFile.Name) +
			sourceFile.LastWriteTime.ToUniversalTime().ToString("yyyy-MM-dd_hh-mm-ss") +
			sourceFile.Extension;
		}
	}

	void CreateReport()
	{
		var dir = new DirectoryInfo(SelectedResultsFolderLocation);
		using (new EditorGUI.DisabledScope(!dir.EnumerateFiles("*.xml").Any()))
			if (GUILayout.Button("Create Report"))
			{
				var cmd = new System.Diagnostics.Process();
				cmd.StartInfo.FileName = "cmd.exe";
				cmd.StartInfo.RedirectStandardInput = true;
				cmd.StartInfo.RedirectStandardOutput = true;
				cmd.StartInfo.CreateNoWindow = true;
				cmd.StartInfo.UseShellExecute = false;
				cmd.Start();

				cmd.StandardInput.WriteLine($"cd {Path.Combine(Environment.CurrentDirectory, ReporterFolderName)}");
				var baseline = Baseline;
				cmd.StandardInput.WriteLine(
					$"dotnet {ReporterDllName} " +
					$"--results=\"{SelectedResultsFolderName}\" " +
					(baseline != null ?
						$"--baseline=\"{baseline}\" " :
						" "));
				cmd.StandardInput.Flush();
				cmd.StandardInput.Close();
				cmd.WaitForExit();
				Debug.Log(cmd.StandardOutput.ReadToEnd());
			}
	}

	void OpenReport()
	{
		using (new EditorGUI.DisabledScope(Report == null))
			if (GUILayout.Button("Open Report"))
			{
				System.Diagnostics.Process.Start(Report.FullName);
			}
	}

	void OpenReporterLocation()
	{
		if (GUILayout.Button("Open Reporter Location"))
		{
			System.Diagnostics.Process.Start(ReporterFolderLocation);
		}
	}

	void SelectedResults()
	{
		EditorGUILayout.BeginVertical();
		BaselineResult();
		SelectedBenchmarkResults();
		EditorGUILayout.EndVertical();
	}

	Vector2 unselectedScrollPos;
	void UnselectedBenchmarkResults()
	{
		EditorGUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Unselected Results", EditorStyles.boldLabel);
		unselectedScrollPos = EditorGUILayout.BeginScrollView(unselectedScrollPos);
		MoveableList(UnselectedResultsFolder.EnumerateFiles(), SelectedResultsFolderLocation, BaselineResultFolderLocation);
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	Vector2 selectedScrollPos;
	void SelectedBenchmarkResults()
	{
		EditorGUILayout.BeginVertical("Box");
		EditorGUILayout.LabelField("Selected Results", EditorStyles.boldLabel);
		selectedScrollPos = EditorGUILayout.BeginScrollView(selectedScrollPos);
		MoveableList(SelectedResultsFolder.EnumerateFiles(), UnselectedResultsFolderLocation, BaselineResultFolderLocation);
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}

	void MoveableList(IEnumerable<FileInfo> files, string moveFolder, string shiftMoveFolder)
	{
		var shift = Event.current.shift;
		using (new EditorGUI.DisabledScope(shift && Baseline != null))
			foreach (var file in files)
			{
				if (GUILayout.Button(file.Name))
				{
					File.Move(file.FullName, shift ?
						Path.Combine(shiftMoveFolder, file.Name) :
						Path.Combine(moveFolder, file.Name));
				}
			}
	}

	void BaselineResult()
	{
		EditorGUILayout.BeginHorizontal("Box");
		EditorGUILayout.LabelField("Selected Baseline Result", EditorStyles.boldLabel);

		using (new EditorGUI.DisabledScope(Baseline == null))
			if (GUILayout.Button(Baseline != null ?
				Baseline.Name :
				"No Baseline Selected"))
			{
				File.Move(Baseline.FullName, Path.Combine(UnselectedResultsFolderLocation, Baseline.Name));
			}
		EditorGUILayout.EndHorizontal();
	}

	void CreateFolders()
	{
		CreateFolder(SelectedResultsFolderLocation);
		CreateFolder(UnselectedResultsFolderLocation);
		CreateFolder(BaselineResultFolderLocation);
		CreateFolder(BenchmarkReportFolderLocation);
		void CreateFolder(string folder)
		{
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
		}
	}
}