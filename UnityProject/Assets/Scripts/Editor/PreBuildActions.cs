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
		EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");

		if (!SpawnListBuild())
		{
			Debug.LogError("Could not cache prefabs for SpawnList. Unknown Error");
			return;
		}

		if (!CacheTiles())
		{
			Debug.LogError("Could not cache tiles for TileManager. Unknown Error");
			return;
		}

		BuildPipeline.BuildPlayer(obj);
	}

	private static bool CacheTiles()
	{
		var tileManager = GameObject.FindObjectOfType<TileManager>();
		if (tileManager.CacheAllAssets())
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	private static bool SpawnListBuild()
	{
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