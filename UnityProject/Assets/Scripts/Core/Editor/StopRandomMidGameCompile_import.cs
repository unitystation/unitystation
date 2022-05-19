using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class StopRandomMidGameCompile_import
{
	// register an event handler when the class is initialized
	static StopRandomMidGameCompile_import()
	{
		EditorApplication.playModeStateChanged += StateChange;
	}

	private static void StateChange(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.ExitingPlayMode)
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
		}
		else if (state == PlayModeStateChange.EnteredPlayMode)
		{
			AssetDatabase.StartAssetEditing();
		}
	}
}
#endif
