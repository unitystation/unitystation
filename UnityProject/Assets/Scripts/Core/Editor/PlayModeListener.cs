using System;
using Logs;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlayModeListener
{
	static PlayModeListener()
	{
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.ExitingPlayMode)
		{
			try
			{
				for (int i = 0; i < SceneManager.sceneCount; i++)
				{
					Scene scene = SceneManager.GetSceneAt(i);
					GameObject[] rootObjects = scene.GetRootGameObjects();
					foreach (GameObject obj in rootObjects)
					{
						Object.DestroyImmediate(obj);
					}
				}


				CleanupUtil.EndRoundCleanup();
				CleanupUtil.CleanupInbetweenScenes();
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}
	}
}