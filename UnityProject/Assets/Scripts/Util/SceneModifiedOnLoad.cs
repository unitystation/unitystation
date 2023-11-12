#if UNITY_EDITOR
using System.Collections.Generic;
using Logs;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Util
{
	public static class SceneModifiedOnLoad
	{
		private static readonly HashSet<Scene> ModifiedScenes = new();

		/// <summary>
		/// Request to save a modified scene after the scene has finished loading. This only applies to a scene
		/// that is still loading and not currently in play mode.
		/// </summary>
		public static void RequestSaveScene(Scene scene)
		{
			if (Application.isPlaying || ModifiedScenes.Contains(scene) || scene.isLoaded) return;

			if (ModifiedScenes.Count == 0) EditorSceneManager.sceneOpened += SaveAfterLoaded;

			ModifiedScenes.Add(scene);
		}

		private static void SaveAfterLoaded(Scene scene, OpenSceneMode mode)
		{
			if (ModifiedScenes.Remove(scene) == false) return;

			if (ModifiedScenes.Count == 0) EditorSceneManager.sceneOpened -= SaveAfterLoaded;

			Loggy.Log($"{scene.name}: Scene was modified while loading and saved.", Category.Editor);
			EditorSceneManager.SaveScene(scene);
		}
	}
}
#endif