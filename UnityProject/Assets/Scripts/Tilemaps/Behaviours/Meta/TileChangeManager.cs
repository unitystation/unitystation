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

	private SubsystemManager subsystemManager;


	private readonly Dictionary<Layer, Dictionary<Vector3Int, TileChangeEntry>> PresentTiles =
		new Dictionary<Layer, Dictionary<Vector3Int, TileChangeEntry>>();

	private readonly Dictionary<Layer, Dictionary<Vector3Int, List<TileChangeEntry>>> MultilayerPresentTiles =
		new Dictionary<Layer, Dictionary<Vector3Int, List<TileChangeEntry>>>();

	/// <summary>
	/// subsystem manager for these tiles
	/// </summary>
	public SubsystemManager SubsystemManager => subsystemManager;

	private InteractableTiles interactableTiles;

	/// <summary>
	/// interactable tiles component for these tiles.
	/// </summary>
	public InteractableTiles InteractableTiles => interactableTiles;

	public MetaTileMap MetaTileMap => metaTileMap;


	private void Awake()
	{
		metaTileMap = GetComponentInChildren<MetaTileMap>();
		subsystemManager = GetComponent<SubsystemManager>();
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

	// [Server]
	// public void UpdateTile(Vector3Int cellPosition, TileType tileType, string tileName,
	// 	Matrix4x4? transformMatrix = null, Color? color = null)
	// {
	// 	if (IsDifferent(cellPosition, tileType, tileName, transformMatrix, color))
	// 	{
	// 		InternalUpdateTile(cellPosition, tileType, tileName, transformMatrix, color);
	//
	// 		AlertClients(cellPosition, tileType, tileName, transformMatrix, color);
	//
	// 		AddToChangeList(cellPosition, tileType: tileType, tileName: tileName, transformMatrix : transformMatrix , color: color);
	// 	}
	// }


	// [Server]
	// public Vector3Int UpdateTile(Vector3Int cellPosition, LayerTile layerTile, Matrix4x4? transformMatrix = null,
	// 	Color? color = null)
	// {
	// 	Vector3Int Vector3Int = Vector3Int.zero;
	// 	if (IsDifferent(cellPosition, layerTile, transformMatrix, color))
	// 	{
	// 		Vector3Int = InternalUpdateTile(cellPosition, layerTile,transformMatrix, color);
	//
	// 		AlertClients(cellPosition, layerTile.TileType, layerTile.name, transformMatrix, color);
	//
	// 		AddToChangeList(cellPosition, layerTile, transformMatrix : transformMatrix , color: color);
	// 	}
	//
	// 	return Vector3Int;
	// }

	/// <summary>
	/// Removes the tile at the indicated cell position. By default all tiles in that layer
	/// at all z levels will be removed. If removeAll is true, then only the tile at cellPosition.z will
	/// be removed.
	/// </summary>
	/// <param name="cellPosition"></param>
	/// <param name="layerType"></param>
	// /// <returns></returns>
	// [Server]
	// public void RemoveTile(Vector3Int position, LayerType layerType)
	// {
	// 	if (metaTileMap.Layers.TryGetValue(layerType, out var layer))
	// 	{
	// 		metaTileMap.RemoveTileWithlayer(position, layerType);
	// 	}
	// }

	//#region Overlays


	/// <summary>
	/// Dynamically adds overlays to tile position, from tile name, remember that OverlayName must be the same as the tile name
	/// </summary>
	// [Server]
	// public void AddOverlay(Vector3Int cellPosition, TileType tileType, string tileName,
	// 	Matrix4x4? transformMatrix = null, Color? color = null)
	// {
	// 	var overlayTile = TileManager.GetTile(tileType, tileName) as OverlayTile;
	// 	AddOverlay(cellPosition, overlayTile, transformMatrix, color);
	// }

	// [Server]
	// public void RemoveOverlaysOfType(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType, bool onlyIfCleanable = false)
	// {
	// 	cellPosition.z = 0;
	//
	// 	var overlayPos = metaTileMap.GetOverlayPosByType(cellPosition, layerType, overlayType);
	// 	if(overlayPos == null || overlayPos.Count == 0) return;
	//
	// 	foreach (var overlay in overlayPos)
	// 	{
	// 		cellPosition = overlay;
	//
	// 		if (onlyIfCleanable)
	// 		{
	// 			//only remove it if it's a cleanable tile
	// 			var tile = metaTileMap.GetTile(cellPosition, layerType) as OverlayTile;
	// 			//it's not an overlay tile or it's not cleanable so don't remove it
	// 			if (tile == null || !tile.IsCleanable) continue;
	// 		}
	//
	// 		RemoveTile(cellPosition, layerType);
	//
	// 		AddToChangeList(cellPosition, layerType);
	// 	}
	// }
	//
	// [Server]
	// public void RemoveAllOverlays(Vector3Int cellPosition, LayerType layerType, bool onlyIfCleanable = false)
	// {
	// 	cellPosition.z = 0;
	//
	// 	var overlayPos = metaTileMap.GetAllOverlayPos(cellPosition, layerType);
	// 	if(overlayPos == null || overlayPos.Count == 0) return;
	//
	// 	foreach (var overlay in overlayPos)
	// 	{
	// 		cellPosition = overlay;
	//
	// 		if (onlyIfCleanable)
	// 		{
	// 			//only remove it if it's a cleanable tile
	// 			var tile = metaTileMap.GetTile(cellPosition, layerType) as OverlayTile;
	// 			//it's not an overlay tile or it's not cleanable so don't remove it
	// 			if (tile == null || !tile.IsCleanable) continue;
	// 		}
	//
	// 		RemoveTile(cellPosition, layerType);
	//
	// 		AddToChangeList(cellPosition, layerType);
	// 	}
	// }
	//
	// [Server]
	// public void RemoveFloorWallOverlaysOfType(Vector3Int cellPosition, OverlayType overlayType, bool onlyIfCleanable = false)
	// {
	// 	RemoveOverlaysOfType(cellPosition, LayerType.Floors, overlayType, onlyIfCleanable);
	// 	RemoveOverlaysOfType(cellPosition, LayerType.Walls, overlayType, onlyIfCleanable);
	// }
	//
	// [Server]
	// public List<OverlayTile> GetAllOverlayTiles(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType)
	// {
	// 	return metaTileMap.GetOverlayTilesByType(cellPosition, layerType, overlayType);
	// }
	//
	// [Server]
	// public bool HasOverlay(Vector3Int cellPosition, OverlayTile overlayTile)
	// {
	// 	return metaTileMap.HasOverlay(cellPosition, overlayTile.LayerType, overlayTile);
	// }
	//
	// [Server]
	// public bool HasOverlay(Vector3Int cellPosition, TileType tileType, string overlayName)
	// {
	// 	var overlayTile = TileManager.GetTile(tileType, overlayName) as OverlayTile;
	// 	if (overlayTile == null) return false;
	//
	// 	return metaTileMap.HasOverlay(cellPosition, overlayTile.LayerType, overlayTile);
	// }
	//
	// [Server]
	// public bool HasOverlayOfType(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType)
	// {
	// 	return metaTileMap.HasOverlayOfType(cellPosition, layerType, overlayType);
	// }
	//
	// /// <summary>
	// /// Gets the colour of the first tile of this type
	// /// </summary>
	// [Server]
	// public Color? GetColourOfFirstTile(Vector3Int cellPosition, OverlayType overlayType, LayerType layerType)
	// {
	// 	var overlays = metaTileMap.GetOverlayPosByType(cellPosition, layerType, overlayType);
	// 	if (overlays.Count == 0) return null;
	//
	// 	return metaTileMap.GetColour(overlays.First(), layerType);
	// }
	//
	// #endregion
	//
	//
	// [Server]
	// public LayerTile GetLayerTile(Vector3Int cellPosition, LayerType layerType)
	// {
	// 	return metaTileMap.GetTile(cellPosition, layerType);
	// }
	//
	// private void AlertClients(Vector3Int position, TileType tileType, string tileName,
	// 	Matrix4x4? transformMatrix = null, Color? color = null)
	// {
	// 	if (color == null)
	// 	{
	// 		color = color.GetValueOrDefault(Color.white);
	// 	}
	//
	// 	if (transformMatrix == null)
	// 	{
	// 		transformMatrix = transformMatrix.GetValueOrDefault(Matrix4x4.identity);
	// 	}
	//
	// 	SpawnSafeThread.UpdateTileMessageSend(networkMatrix.MatrixSync.netId, position, tileType, tileName, (Matrix4x4)transformMatrix, (Color)color);
	// }
	//
	// public void InternalUpdateTile(Vector3Int position, TileType tileType, string tileName,
	// 	Matrix4x4? transformMatrix = null, Color? color = null)
	// {
	// 	LayerTile layerTile = TileManager.GetTile(tileType, tileName);
	// 	metaTileMap.SetTile(position, layerTile, transformMatrix, color);
	// }
	//
	// public Vector3Int InternalUpdateTile(Vector3 position, LayerTile layerTile, Matrix4x4? transformMatrix = null,
	// 	Color? color = null)
	// {
	// 	Vector3Int p = position.RoundToInt();
	//
	// 	return metaTileMap.SetTile(p, layerTile, transformMatrix, color);
	// }
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

	//
	// private bool IsDifferent(Vector3Int position, TileType tileType, string tileName, Matrix4x4? transformMatrix = null,
	// 	Color? color = null)
	// {
	// 	LayerTile layerTile = TileManager.GetTile(tileType, tileName);
	//
	// 	return IsDifferent(position, layerTile, transformMatrix, color);
	// }
	//
	// private bool IsDifferent(Vector3Int position, LayerTile layerTile, Matrix4x4? transformMatrix = null,
	// 	Color? color = null)
	// {
	// 	return metaTileMap.IsDifferent(position, layerTile, layerTile.LayerType, transformMatrix, color);
	// }
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