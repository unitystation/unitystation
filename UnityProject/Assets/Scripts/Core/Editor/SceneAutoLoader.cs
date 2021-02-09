using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
	// Static constructor binds a playmode-changed callback.
	// [InitializeOnLoad] above makes sure this gets executed.
	static SceneAutoLoader()
	{
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}

	// Play mode change callback handles the scene load/reload.
	private static void OnPlayModeChanged(PlayModeStateChange state)
	{
		if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
		{
			if (EditorSceneManager.GetActiveScene().name == "Lobby" ||
			    EditorSceneManager.GetActiveScene().name == "OnlineScene")
			{
				EditorPrefs.SetString("prevEditorScene", "");
				PreviousScene = "";
				return;
			}

			EditorPrefs.SetString("prevEditorScene", EditorSceneManager.GetActiveScene().name);

			// User pressed play -- autoload online scene.
			PreviousScene = EditorSceneManager.GetActiveScene().path;
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				try
				{
					EditorSceneManager.OpenScene(MasterScene);
				}
				catch
				{
					Debug.LogError(string.Format("error: scene not found: {0}", MasterScene));
					EditorApplication.isPlaying = false;

				}
			}
			else
			{
				// User cancelled the save operation -- cancel play as well.
				EditorApplication.isPlaying = false;
			}
		}

		// isPlaying check required because cannot OpenScene while playing
		if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
		{
			// User pressed stop -- reload previous scene.
			try
			{
				EditorPrefs.SetString("prevEditorScene", "");
				if (!string.IsNullOrEmpty(PreviousScene))
				{
					EditorSceneManager.OpenScene(PreviousScene);
				}
			}
			catch
			{
				Debug.LogError(string.Format("error: scene not found: {0}", PreviousScene));
			}
		}
	}

	private const string cEditorPrefPreviousScene = "SceneAutoLoader.PreviousScene";

	private static string MasterScene
	{
		get { return "Assets/Scenes/ActiveScenes/OnlineScene.unity"; }
	}

	private static string PreviousScene
	{
		get { return EditorPrefs.GetString(cEditorPrefPreviousScene, EditorSceneManager.GetActiveScene().path); }
		set { EditorPrefs.SetString(cEditorPrefPreviousScene, value); }
	}
}
