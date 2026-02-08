using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Experimental;
using UnityEngine;

public class DisableAnalytics : IPreprocessBuild
{
	public int callbackOrder
	{
		get { return 10; }
	}


	public void OnPreprocessBuild(BuildTarget target, string path)
	{

		var unityConnectSettings  = new SerializedObject(
			EditorResources.Load<UnityEngine.Object>("ProjectSettings/UnityConnectSettings.asset"));


		var enabled = unityConnectSettings.FindProperty("m_Enabled");
		enabled.boolValue = false;


		var initializeOnStartup = unityConnectSettings.FindProperty("UnityAnalyticsSettings").FindPropertyRelative("m_InitializeOnStartup");
		initializeOnStartup.boolValue = false;

		unityConnectSettings.ApplyModifiedProperties();

		AssetDatabase.SaveAssets();
	}
}
