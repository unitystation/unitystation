using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

public class LavaLandManager : MonoBehaviour
{
	private static LavaLandManager instance;
	public static LavaLandManager Instance => instance;

	public List<LavaLandRandomAreaSO> areaSOs = new List<LavaLandRandomAreaSO>();

	private List<LavaLandData> dataList = new List<LavaLandData>();

	[HideInInspector]
	public List<LavaLandRandomGenScript> randomGenScripts = new List<LavaLandRandomGenScript>();

	public IDictionary<LavaLandAreaSpawnerScript, AreaSizes> SpawnScripts = new Dictionary<LavaLandAreaSpawnerScript, AreaSizes>();

	private IDictionary<GameObject, GameObject> PrefabsUsed = new Dictionary<GameObject, GameObject>();

	private TileChangeManager tileChangeManager;

	//temp stuff, allows for maps to have a teleport to lava land mapped if they want it.:
	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	[HideInInspector]
	public QuantumPad LavaLandBase2;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	[HideInInspector]
	public QuantumPad LavaLandBase1;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	[HideInInspector]
	public QuantumPad LavaLandBase1Connector;

	/// <summary>
	/// Temp until shuttle landings possible
	/// </summary>
	[HideInInspector]
	public QuantumPad LavaLandBase2Connector;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundStarted, SpawnLavaLand);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundStarted, SpawnLavaLand);
	}

	public void SpawnLavaLand()
	{
		if(!CustomNetworkManager.IsServer) return;

		if(MatrixManager.Instance.lavaLandMatrix == null) return;

		StartCoroutine(SpawnLavaLandCo());
	}

	public IEnumerator SpawnLavaLandCo()
	{
		foreach (var script in randomGenScripts)
		{
			if(script == null) continue;

			script.numR = Random.Range(1,7);
			script.DoSim();
		}

		tileChangeManager = MatrixManager.Instance.lavaLandMatrix.transform.parent.GetComponent<TileChangeManager>();

		GenerateStructures();

		MatrixManager.Instance.lavaLandMatrix.transform.parent.GetComponent<OreGenerator>().RunOreGenerator();

		SetQuantumPads();

		Debug.Log("Finished generating LavaLand");

		yield break;
	}

	//Temp until shuttle landings
	private void SetQuantumPads()
	{
		if (LavaLandBase1 != null && LavaLandBase1Connector != null)
		{
			LavaLandBase1.connectedPad = LavaLandBase1Connector;
			LavaLandBase1Connector.connectedPad = LavaLandBase1;
		}

		if (LavaLandBase2 != null && LavaLandBase2Connector != null)
		{
			LavaLandBase2.connectedPad = LavaLandBase2Connector;
			LavaLandBase2Connector.connectedPad = LavaLandBase2;
		}
	}

	public LavaLandRandomAreaSO GetCorrectSOFromSize(AreaSizes size)
	{
		foreach (var areaSO in areaSOs)
		{
			if (areaSO.AreaSize == size)
			{
				return areaSO;
			}
		}

		return null;
	}

	public void GenerateStructures()
	{
		if(SpawnScripts.Count == 0) return;

		foreach (var keyValuePair in SpawnScripts)
		{
			var SO = GetCorrectSOFromSize(keyValuePair.Value);
			if(SO == null) continue;

			dataList = SO.AreaPrefabData.ToList();

			foreach (var data in dataList.Shuffle())
			{
				if(data.isSpecialSite && !keyValuePair.Key.allowSpecialSites) continue;

				//Prefab cache
				if (PrefabsUsed.ContainsKey(data.AreaPrefab))
				{
					SpawnArea(PrefabsUsed[data.AreaPrefab], keyValuePair.Key);
				}
				else
				{
					var prefab = Instantiate(data.AreaPrefab, Vector3.zero, keyValuePair.Key.gameObject.transform.rotation, keyValuePair.Key.transform.parent);
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

		var layers = prefab.GetComponentInChildren<MetaTileMap>().LayersValues;

		var metaMap = prefab.GetComponentInChildren<MetaTileMap>();

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
					var posTarget = gameObjectPos + pos - script.gameObject.transform.parent.parent.parent.position.RoundToInt();

					tileChangeManager.UpdateTile(posTarget, layerTile);
				}
			}

			//Copy and create objects for the new area
			if (layer.LayerType == LayerType.Objects)
			{
				foreach (Transform child in layer.gameObject.transform)
				{
					var childPrefab = Instantiate(child.gameObject, child.transform.position, script.transform.rotation, script.transform.parent);
					childPrefab.transform.position += gameObjectPos;
				}
			}
		}
	}
}