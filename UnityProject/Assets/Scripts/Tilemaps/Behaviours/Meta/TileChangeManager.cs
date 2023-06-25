using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using Messages.Server;
using UnityEngine;
using UnityEngine.Events;
using TileManagement;
using Tilemaps.Behaviours.Layers;

public class TileChangeManager : MonoBehaviour
{
	private MetaTileMap metaTileMap;
	private NetworkedMatrix networkMatrix;

	private TileChangeList changeList = new TileChangeList(); //it is not static so okay

	public Vector3IntEvent OnFloorOrPlatingRemoved = new Vector3IntEvent();

	private MatrixSystemManager subsystemManager;


	private readonly Dictionary<Layer, Dictionary<Vector3Int, TileChangeEntry>> PresentTiles =
		new Dictionary<Layer, Dictionary<Vector3Int, TileChangeEntry>>();

	private readonly Dictionary<Layer, Dictionary<Vector3Int, List<TileChangeEntry>>> MultilayerPresentTiles =
		new Dictionary<Layer, Dictionary<Vector3Int, List<TileChangeEntry>>>();

	/// <summary>
	/// subsystem manager for these tiles
	/// </summary>
	public MatrixSystemManager SubsystemManager => subsystemManager;

	private InteractableTiles interactableTiles;

	/// <summary>
	/// interactable tiles component for these tiles.
	/// </summary>
	public InteractableTiles InteractableTiles => interactableTiles;

	public MetaTileMap MetaTileMap => metaTileMap;


	private void OnDestroy()
	{
		networkMatrix = null;
	}

	private void Awake()
	{
		metaTileMap = GetComponentInChildren<MetaTileMap>();
		subsystemManager = GetComponent<MatrixSystemManager>();
		interactableTiles = GetComponent<InteractableTiles>();
		networkMatrix = GetComponent<NetworkedMatrix>();
	}

	[Server]
	public void UpdateNewPlayer(NetworkConnection requestedBy)
	{
		if (changeList.List.Count > 0)
		{
			UpdateTileMessage.SendTo(gameObject, requestedBy, changeList);
		}
	}

	public TileChangeEntry TryGetPreExisting(Vector3Int position, Layer layer, bool multilayer)
	{
		if (multilayer)
		{
			if (MultilayerPresentTiles.ContainsKey(layer) == false)
			{
				MultilayerPresentTiles[layer] = new Dictionary<Vector3Int, List<TileChangeEntry>>();
			}

			//TODO Tilemap upgrade
			var ZZeroposition = position;
			ZZeroposition.z = 0;

			if (MultilayerPresentTiles[layer].ContainsKey(ZZeroposition) == false)
			{
				MultilayerPresentTiles[layer][ZZeroposition] = new List<TileChangeEntry>();
			}

			var tileLocations = MultilayerPresentTiles[layer][ZZeroposition];
			if (tileLocations.Count > Math.Abs(1 - position.z))
			{
				return tileLocations[Math.Abs(1 - position.z)];
			}
			else
			{
				return null;
			}
		}
		else
		{
			if (PresentTiles.ContainsKey(layer) == false)
			{
				PresentTiles[layer] = new Dictionary<Vector3Int, TileChangeEntry>();
			}

			if (PresentTiles[layer].ContainsKey(position) == false)
			{
				return null;
			}
			else
			{
				return PresentTiles[layer][position];
			}
		}
	}


	public void AddToChangeList(Vector3Int position, LayerType LayerType, Layer layer, TileLocation RelatedTileLocation,
		bool multilayer, bool remove)
	{
		var preExistingTileChange = TryGetPreExisting(position, layer, multilayer);

		if (preExistingTileChange != null)
		{
			if (remove)
			{
				preExistingTileChange.TileChangeToRemove();
			}
			else
			{
				preExistingTileChange.TileChangeToSet(RelatedTileLocation);
			}
			return;
		}


		var TileChange = new TileChangeEntry()
		{
			position = position,
			LayerType = LayerType,
			RelatedTileLocation = RelatedTileLocation,
		};

		if (multilayer == false)
		{
			if (PresentTiles.ContainsKey(layer) == false)
			{
				PresentTiles[layer] = new Dictionary<Vector3Int, TileChangeEntry>();
			}

			PresentTiles[layer][position] = TileChange;
		}
		else
		{
			if (MultilayerPresentTiles.ContainsKey(layer) == false)
			{
				MultilayerPresentTiles[layer] = new Dictionary<Vector3Int, List<TileChangeEntry>>();
			}

			//TODO Tilemap upgrade
			var ZZeroposition = position;
			ZZeroposition.z = 0;

			if (MultilayerPresentTiles[layer].ContainsKey(ZZeroposition) == false)
			{
				MultilayerPresentTiles[layer][ZZeroposition] = new List<TileChangeEntry>();
			}

			var tileLocations = MultilayerPresentTiles[layer][ZZeroposition];
			while ((tileLocations.Count <= Math.Abs(1 - position.z)))
			{
				tileLocations.Add(null);
			}

			tileLocations[Math.Abs(1 - position.z)] = TileChange;
		}

		changeList.List.Add(TileChange);
	}
}

[System.Serializable]
public class TileChangeList
{
	public List<TileChangeEntry> List = new List<TileChangeEntry>();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector3Int position;
	public LayerType LayerType;
	public TileLocation RelatedTileLocation;

	public void TileChangeToRemove()
	{
		RelatedTileLocation = null;
	}

	public void TileChangeToSet(TileLocation NewRelatedTileLocation)
	{
		RelatedTileLocation = NewRelatedTileLocation;
	}
}