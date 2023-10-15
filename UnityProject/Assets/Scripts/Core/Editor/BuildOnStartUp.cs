using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class BuildOnStartUp : MonoBehaviour
{

//#if UNITY_EDITOR

	[InitializeOnLoadMethod]
	public static  void Start()
	{
		//EditorApplication.delayCall += DelayCall.ByNumberOfEditorFrames(10, () => DO());
	}

	public static class DelayCall
	{
		public static EditorApplication.CallbackFunction ByNumberOfEditorFrames(int n, Action a)
		{
			EditorApplication.CallbackFunction callback = null;

			callback = new EditorApplication.CallbackFunction(() =>
			{
				if (n-- <= 0)
				{
					a();
				}
				else
				{
					EditorApplication.delayCall += callback;
				}
			});

			return callback;
		}
	}


	public static void DO()
	{

		string path = @"Q:\Fast programmes\ss13 development\unitystation\UnityProject\Assets\Tests\CodeScanning\Build";
		string[] levels = new string[] {};

		// Build player.
		BuildPipeline.BuildPlayer(levels, path + "/BuiltGame.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);


		var data = System.Environment.GetCommandLineArgs();

		if (data.Contains("-ScanTes")){
		}
	}
//#endif
}
