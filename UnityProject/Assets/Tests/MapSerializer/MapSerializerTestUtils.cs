using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using US13.MapSaver;
using US13.Tilemaps.Behaviours.Layers;
using MapSaverClass = US13.MapSaver.MapSaver;

namespace Tests.MapSerializer
{
	public static class MapSerializerTestUtils
	{
		private const string EmptyMapScene = "Assets/Scenes/DevScenes/EmptyMap.unity";

		public static readonly JsonSerializerSettings SaveSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
			Formatting = Formatting.Indented
		};

		public static void OpenEmptyMap()
		{
			EditorSceneManager.OpenScene(EmptyMapScene);
		}

		public static MapSaverClass.MapData DeserializeRoom(string relativeToRooms)
		{
			var abs = Path.Combine(Application.dataPath, "StreamingAssets/Rooms", relativeToRooms);
			return JsonConvert.DeserializeObject<MapSaverClass.MapData>(File.ReadAllText(abs));
		}

		public static MapSaverClass.MapData DeserializeJson(string json)
		{
			return JsonConvert.DeserializeObject<MapSaverClass.MapData>(json);
		}

		public static List<MetaTileMap> LoadIntoFreshMatrix(MapSaverClass.MapData mapData)
		{
			MapSaverClass.CodeClass.ThisCodeClass.Reset();
			var before = new HashSet<MetaTileMap>(Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None));
			RunCoroutineInEditor(MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData, TestLoad: true));
			return Object.FindObjectsByType<MetaTileMap>(FindObjectsSortMode.None)
				.Where(m => before.Contains(m) == false)
				.ToList();
		}

		public static string SaveToJson(List<MetaTileMap> matrices)
		{
			var ordered = new List<MetaTileMap>(matrices);
			ordered.Reverse();
			var map = MapSaverClass.SaveMap(ordered, false, ordered[0].name);
			return JsonConvert.SerializeObject(map, SaveSettings);
		}

		public static GameObject SpawnPrefab(string prefabPath)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		}

		public static void RunCoroutineInEditor(IEnumerator coroutine)
		{
			var stack = new Stack<IEnumerator>();
			stack.Push(coroutine);
			while (stack.Count > 0)
			{
				IEnumerator top = stack.Peek();
				if (top.MoveNext() == false)
				{
					stack.Pop();
					continue;
				}

				if (top.Current is IEnumerator nested)
				{
					stack.Push(nested);
				}
			}
		}
	}
}
