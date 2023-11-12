using Logs;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Util
{
	//[InitializeOnLoad]
	public class PreBuildActions : IPreprocessBuildWithReport
	{
		private Scene scene;
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
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

			if (SpawnListBuild() == false)
			{
				Loggy.LogError("Could not cache prefabs for SpawnList. Unknown Error", Category.Editor);
				return;
			}

			if (CacheTiles() == false)
			{
				Loggy.LogError("Could not cache tiles for TileManager. Unknown Error", Category.Editor);
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

			return false;
		}

		private bool SpawnListBuild()
		{
			var spawnListMonitor = GameObject.FindObjectOfType<SpawnListMonitor>();
			if (spawnListMonitor.GenerateSpawnList())
			{
				PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
				return true;
			}

			return false;
		}
	}
}
