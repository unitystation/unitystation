using Logs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Core.Editor
{

	/// <summary>
	/// Scene auto loader.
	/// </summary>
	/// <description>
	/// This class adds a File > Scene Autoload menu containing options to select
	/// a "master scene" enable it to be auto-loaded when the user presses play
	/// in the editor. When enabled, the selected scene will be loaded on play,
	/// then the original scene will be reloaded on stop.
	///
	/// Based on an idea on this thread:
	/// http://forum.unity3d.com/threads/157502-Executing-first-scene-in-build-settings-when-pressing-play-button-in-editor
	/// </description>
	[InitializeOnLoad]
	static class SceneAutoLoader
	{
		private static string MasterScene => "Assets/Scenes/ActiveScenes/OnlineScene.unity";

		private static string PreviousSceneName
		{
			get => EditorPrefs.GetString("prevEditorScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
			set => EditorPrefs.SetString("prevEditorScene", value);
		}

		private static string PreviousScenePath
		{
			get => EditorPrefs.GetString("SceneAutoLoader.PreviousScenePath", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
			set => EditorPrefs.SetString("SceneAutoLoader.PreviousScenePath", value);
		}

		// Static constructor binds a playmode-changed callback.
		// [InitializeOnLoad] above makes sure this gets executed.
		static SceneAutoLoader()
		{
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}

		// Play mode change callback handles the scene load/reload.
		private static void OnPlayModeChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				OnEnteringPlayMode();
			}

			if (state == PlayModeStateChange.EnteredEditMode)
			{
				OnExitedPlayMode();
			}
		}

		private static void OnEnteringPlayMode()
		{
			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("InitTestScene"))
			{
				PreviousSceneName = "RRT CleanStation"; //Sets it to the Test statistician to load
				return; //tests are running do not interfere
			}

			// User pressed play -- check for unsaved changes.
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
			{
				// User cancelled the save operation -- cancel play as well.
				EditorApplication.isPlaying = false;
				return;
			}

			// Save current scene to return to it after play mode.
			PreviousSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			PreviousScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

			// if the scene is startup or lobby, load lobby scene
			var initialScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
			if (initialScene.Contains("StartUp") || initialScene.Contains("Lobby"))
			{
				TryOpenScene(initialScene);
			}
			else
			{
				TryOpenScene(MasterScene);
			}
		}

		private static void OnExitedPlayMode()
		{
			// User pressed stop -- reload previous scene.
			if (string.IsNullOrEmpty(PreviousScenePath)) return;

			TryOpenScene(PreviousScenePath);
			PreviousScenePath = string.Empty;
		}

		private static void TryOpenScene(string scenePath)
		{
			try
			{
				EditorSceneManager.OpenScene(scenePath);
			}
			catch
			{
				Loggy.LogError($"Tried to autoload scene, but scene not found: {scenePath}", Category.Editor);
			}
		}
	}
}