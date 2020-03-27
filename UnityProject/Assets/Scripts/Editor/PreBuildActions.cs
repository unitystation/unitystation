using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class PreBuildActions
{
	static PreBuildActions()
	{
		Debug.Log("Init pre build checker");
		BuildPlayerWindow.RegisterBuildPlayerHandler(PreChecks);
	}

	public static void PreChecks(BuildPlayerOptions obj)
	{
		if (!SpawnListBuild())
		{
			Debug.LogError("Could not cache prefabs for SpawnList. Unknown Error");
			return;
		}

		BuildPipeline.BuildPlayer(obj);
	}

	private static bool SpawnListBuild()
	{
		EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
		var spawnListMonitor = GameObject.FindObjectOfType<SpawnListMonitor>();
		if (spawnListMonitor.GenerateSpawnList())
		{
			return EditorSceneManager.SaveOpenScenes();
		}
		else
		{
			return false;
		}
	}
}