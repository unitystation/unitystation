using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Scenes;
using Objects.Science;
using ScriptableObjects;
using TileManagement;
using UnityEngine;

public class LavaLandGenerator : MonoBehaviour
{
	private List<LavaLandData> dataList = new List<LavaLandData>();

	[HideInInspector] public List<LavaLandRandomGenScript> randomGenScripts = new List<LavaLandRandomGenScript>();

	public IDictionary<LavaLandAreaSpawnerScript, AreaSizes> SpawnScripts =
		new Dictionary<LavaLandAreaSpawnerScript, AreaSizes>();

	private IDictionary<GameObject, GameObject> PrefabsUsed = new Dictionary<GameObject, GameObject>();

	public TileChangeManager tileChangeManager;

	public OreGenerator OreGenerator;

	private void OnEnable()
	{
		EventManager.AddHandler(Event.ScenesLoadedServer, SpawnLavaLand);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.ScenesLoadedServer, SpawnLavaLand);
	}

	public void SpawnLavaLand()
	{
		if (!CustomNetworkManager.IsServer) return;

		if (MatrixManager.Instance.lavaLandMatrix == null) return;

		StartCoroutine(SpawnLavaLandCo());
	}

	public IEnumerator SpawnLavaLandCo()
	{
		foreach (var script in randomGenScripts)
		{
			if (script == null) continue;

			script.numR = Random.Range(1, 7);
			script.DoSim(); //Thread okay
		}

		yield return null;

		GenerateStructures();//TODO Thread BAD Needs to be changed with map tiles Saveer loader thing
		yield return null;
		OreGenerator.RunOreGenerator(); //TODO Thread BAD Fix WaitForNetId and getComponent

		LavaLandManager.Instance.SetQuantumPads();

		Logger.Log("Finished generating LavaLand", Category.Round);

		yield break;
	}



	public void GenerateStructures()
	{
		if (SpawnScripts.Count == 0) return;

		foreach (var keyValuePair in SpawnScripts)
		{
			var SO = LavaLandManager.Instance.GetCorrectSOFromSize(keyValuePair.Value);
			if (SO == null) continue;
			if (keyValuePair.Key == null) continue;

			dataList = SO.AreaPrefabData.ToList();

			foreach (var data in dataList.Shuffle())
			{
				if (data.isSpecialSite && !keyValuePair.Key.allowSpecialSites) continue;
				if (data.AreaPrefab == null) continue;

				//Prefab cache
				if (PrefabsUsed.ContainsKey(data.AreaPrefab))
				{
					SpawnArea(PrefabsUsed[data.AreaPrefab], keyValuePair.Key);
				}
				else
				{
					var prefab = Instantiate(data.AreaPrefab, Vector3.zero, //Thread BAD
						keyValuePair.Key.gameObject.transform.rotation, keyValuePair.Key.transform.parent);
					prefab.transform.parent = null;
					PrefabsUsed.Add(data.AreaPrefab, prefab);
					SpawnArea(prefab, keyValuePair.Key);
				}

				if (data.SpawnOnceOnly)
				{
					dataList.Remove(data);
					Destroy(PrefabsUsed[data.AreaPrefab]);
					PrefabsUsed.Remove(data.AreaPrefab);
				}

				break;
			}

			Destroy(keyValuePair.Key.gameObject);
		}

		SpawnScripts.Clear();

		//Delete prefab cache after use
		foreach (var prefabPairs in PrefabsUsed)
		{
			Destroy(prefabPairs.Value);
		}

		PrefabsUsed.Clear();
	}

	public void SpawnArea(GameObject prefab, LavaLandAreaSpawnerScript script)
	{
		Vector3Int minPosition = Vector3Int.one * int.MaxValue;
		Vector3Int maxPosition = Vector3Int.one * int.MinValue;

		var layers = prefab.GetComponentInChildren<MetaTileMap>().LayersValues;   //Thread BAD

		var metaMap = prefab.GetComponentInChildren<MetaTileMap>(); //Thread BAD


		for (var i = 0; i < layers.Length; i++)
		{
			BoundsInt layerBounds = layers[i].Bounds;
			if (layerBounds.x == 0 && layerBounds.y == 0)
			{
				continue; // Has no tiles
			}

			minPosition = Vector3Int.Min(layerBounds.min, minPosition);
			maxPosition = Vector3Int.Max(layerBounds.max, maxPosition);
		}

		var bounds = new BoundsInt(minPosition, maxPosition - minPosition);

		var gameObjectPos = script.gameObject.WorldPosServer().RoundToInt();

		foreach (var layer in layers)
		{
			foreach (var pos in bounds.allPositionsWithin)
			{
				var layerTile = metaMap.GetTile(metaMap.WorldToCell(metaMap.LocalToWorld(pos)), layer);

				if (layerTile != null)
				{
					var posTarget = gameObjectPos + pos -
					                script.gameObject.transform.parent.parent.parent.position.RoundToInt(); //Thread BAD


					tileChangeManager.MetaTileMap.SetTile(posTarget, layerTile); //Thread BAD

				}
			}

			//Copy and create objects for the new area
			if (layer.LayerType == LayerType.Objects)
			{
				foreach (Transform child in layer.gameObject.transform)
				{
					var childPrefab = Instantiate(child.gameObject, child.transform.position, script.transform.rotation,
						script.transform.parent); //Thread BAD
					childPrefab.transform.position += gameObjectPos;
				}
			}
		}
	}
}