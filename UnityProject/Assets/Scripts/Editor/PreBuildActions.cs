using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//[InitializeOnLoad]
public class PreBuildActions : IPreprocessBuild
{
	private Scene scene;
	public int callbackOrder { get { return 0; } }
	public void OnPreprocessBuild(BuildTarget target, string path) {
		// Do the preprocessing here
		PreChecks();
	}
//	static PreBuildActions()
//	{
//		Debug.Log("Init pre build checker");
//		BuildPlayerWindow.RegisterBuildPlayerHandler(PreChecks);
//	}

	public void PreChecks()
	{
		scene = EditorSceneManager.OpenScene("Assets/Scenes/ActiveScenes/Lobby.unity");

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

	//	BuildPipeline.BuildPlayer(obj);
	}

	private bool CacheTiles()
	{
		var tileManager = GameObject.FindObjectOfType<TileManager>();
		if (tileManager.CacheAllAssets())
		{
			PrefabUtility.ApplyPrefabInstance(tileManager.gameObject, InteractionMode.AutomatedAction);
			EditorSceneManager.MarkSceneDirty(scene);
			return EditorSceneManager.SaveScene(scene);
		}
		else
		{
			return false;
		}
	}

	private bool SpawnListBuild()
	{
		var spawnListMonitor = GameObject.FindObjectOfType<SpawnListMonitor>();
		if (spawnListMonitor.GenerateSpawnList())
		{
			PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
			return true;
		}
		else
		{
			return false;
		}
	}
}