using System;
using System.Collections.Generic;
using System.Linq;
using Core.Physics;
using Cysharp.Threading.Tasks;
using Items;
using Managers;
using Mapping.Move;
using Objects;
using TileMap.Behaviours;
#if true
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mapping.Move
{
	public class RandomSpawnPointToLocationMakers : ItemMatrixSystemInit
	{
		[SerializeField]
		private List<SpawnPointPossibility> spawnPointPossibilities = new List<SpawnPointPossibility>();

		public List<SpawnPointPossibility> SpawnPointPossibilities => spawnPointPossibilities;

		public override async UniTask Initialize()
		{
			await UniTask.WaitForSeconds(1); // maybe waiting should give some time for registerTiles to set their correct positions?
			if (CustomNetworkManager.IsServer == false) return;
			foreach (var point in spawnPointPossibilities)
			{
				if (DMMath.Prob(point.ChanceToSpawn) == false) continue;
				var item = Spawn.ServerPrefab(point.ObjectToSpawn, parent: metaTileMap.ObjectLayer.transform, count: point.Count);
				foreach (var obj in item.GameObjects)
				{
					var physics = obj.GetComponent<UniversalObjectPhysics>();
					GameManager.Instance.MoveToLocationMarker(point.LocationID, physics, point.CheckForUnoccupiedTiles);
					ThingsToDoAfterMove(physics, obj);
				}
			}
		}

		private void ThingsToDoAfterMove(UniversalObjectPhysics physics, GameObject obj)
		{
			if (obj.TryGetComponent<RandomItemSpot>(out var itemSpot))
			{
				itemSpot.SpawnRandomItems(); // doesn't want to work for some odd reason.
			}
			var storage = physics.registerTile.Matrix.GetFirst<ObjectContainer>(physics.registerTile.LocalPosition, CustomNetworkManager.IsServer);
			if (storage != null)
			{
				storage.GatherObjects();
			}
		}

		[Serializable]
		public class SpawnPointPossibility
		{
			public int LocationID = 0;
			[Range(1, 75)] public int Count = 1;
			[Range(0, 100)] public float ChanceToSpawn = 50;
			public GameObject ObjectToSpawn;
			public bool CheckForUnoccupiedTiles = false;
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Mapping.Move.RandomSpawnPointToLocationMakers))]
public class RandomSpawnPointToLocationMakersEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var script = (RandomSpawnPointToLocationMakers)target;

		foreach (var point in script.SpawnPointPossibilities)
		{
			var matchingMarkers = FindObjectsByType<LocationMarker>(FindObjectsSortMode.None)
				.Where(marker => marker.LocationID == point.LocationID).ToArray();
			if (matchingMarkers.Length == 0)
			{
				EditorGUILayout.HelpBox(
					$"No LocationMarker found with LocationID {point.LocationID} for object {point.ObjectToSpawn.name}", MessageType.Warning);
			}
			else
			{
				if (GUILayout.Button($"Select and Focus LocationMarkers with ID {point.LocationID} for {point.ObjectToSpawn.name}"))
				{
					Selection.objects = matchingMarkers.Select(marker => marker.gameObject).ToArray<Object>();
					SceneView.lastActiveSceneView.FrameSelected();
				}
			}
		}
	}
}
#endif
