using System;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using Messages.Client.NewPlayer;
using Messages.Server;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Objects;
using TileManagement;
using Tilemaps.Behaviours.Layers;

public class TileChangeManager : MonoBehaviour
{
	private MetaTileMap metaTileMap;
	private NetworkedMatrix networkMatrix;

	private TileChangeList changeList = new TileChangeList();

	public Vector3IntEvent OnFloorOrPlatingRemoved = new Vector3IntEvent();

	private SubsystemManager subsystemManager;

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

	[Server]
	public void UpdateTile(Vector3Int cellPosition, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		if (IsDifferent(cellPosition, tileType, tileName, transformMatrix, color))
		{
			InternalUpdateTile(cellPosition, tileType, tileName, transformMatrix, color);

			AlertClients(cellPosition, tileType, tileName, transformMatrix, color);

			AddToChangeList(cellPosition, tileType: tileType, tileName: tileName, transformMatrix : transformMatrix , color: color);
		}
	}


	[Server]
	public Vector3Int UpdateTile(Vector3Int cellPosition, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		Vector3Int Vector3Int = Vector3Int.zero;
		if (IsDifferent(cellPosition, layerTile, transformMatrix, color))
		{
			Vector3Int = InternalUpdateTile(cellPosition, layerTile,transformMatrix, color);

			AlertClients(cellPosition, layerTile.TileType, layerTile.name, transformMatrix, color);

			AddToChangeList(cellPosition, layerTile, transformMatrix : transformMatrix , color: color);
		}

		return Vector3Int;
	}

	/// <summary>
	/// Used for the underfloor layer to reduce complexity on the main UpdateTile Function
	/// </summary>
	/// <param name="cellPosition"></param>
	/// <param name="layerTile"></param>
	[Server]
	public void UnderfloorUpdateTile(Vector3Int cellPosition, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		AlertClients(cellPosition, layerTile.TileType, layerTile.name, transformMatrix, color);
		AddToChangeList(cellPosition, layerTile, transformMatrix: transformMatrix, color : color);
	}

	[Server]
	public void RemoveTile(Vector3Int cellPosition)
	{
		foreach (var layerType in metaTileMap.LayersKeys)
		{
			RemoveTile(cellPosition, layerType);
		}
	}

	/// <summary>
	/// Removes the tile at the indicated cell position. By default all tiles in that layer
	/// at all z levels will be removed. If removeAll is true, then only the tile at cellPosition.z will
	/// be removed.
	/// </summary>
	/// <param name="cellPosition"></param>
	/// <param name="layerType"></param>
	/// <returns></returns>
	[Server]
	public LayerTile RemoveTile(Vector3Int cellPosition, LayerType layerType)
	{
		var layerTile = metaTileMap.GetTile(cellPosition, layerType);
		if (metaTileMap.HasTile(cellPosition, layerType))
		{
			InternalRemoveTile(cellPosition, layerType);

			SpawnSafeThread.RemoveTileMessageSend(networkMatrix.MatrixSync.netId, cellPosition, layerType);

			AddToChangeList(cellPosition, layerType);

			if (layerType == LayerType.Floors || layerType == LayerType.Base)
			{
				OnFloorOrPlatingRemoved.Invoke(cellPosition);
			}

			RemoveOverlaysOfType(cellPosition, LayerType.Effects, OverlayType.Damage);

			return layerTile;
		}

		return layerTile;
	}

	#region Overlays

	/// <summary>
	/// Dynamically adds overlays to tile position
	/// </summary>
	[Server]
	public void AddOverlay(Vector3Int cellPosition, OverlayTile overlayTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		//use remove methods to remove overlay instead
		if(overlayTile == null) return;

		cellPosition.z = 0;

		//Dont add the same overlay twice
		if(HasOverlay(cellPosition, overlayTile)) return;

		var overlayPos = metaTileMap.GetFreeOverlayPos(cellPosition, overlayTile.LayerType);
		if (overlayPos == null) return;

		cellPosition = overlayPos.Value;

		InternalUpdateTile(cellPosition, overlayTile, transformMatrix, color);

		AlertClients(cellPosition, overlayTile.TileType, overlayTile.OverlayName, transformMatrix, color);

		AddToChangeList(cellPosition, overlayTile.LayerType,  overlayTile.TileType, overlayTile.OverlayName, transformMatrix : transformMatrix,color : color);
	}

	/// <summary>
	/// Dynamically adds overlays to tile position, from tile name, remember that OverlayName must be the same as the tile name
	/// </summary>
	[Server]
	public void AddOverlay(Vector3Int cellPosition, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		var overlayTile = TileManager.GetTile(tileType, tileName) as OverlayTile;
		AddOverlay(cellPosition, overlayTile, transformMatrix, color);
	}

	[Server]
	public void RemoveOverlaysOfType(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType, bool onlyIfCleanable = false)
	{
		cellPosition.z = 0;

		var overlayPos = metaTileMap.GetOverlayPosByType(cellPosition, layerType, overlayType);
		if(overlayPos == null || overlayPos.Count == 0) return;

		foreach (var overlay in overlayPos)
		{
			cellPosition = overlay;

			if (onlyIfCleanable)
			{
				//only remove it if it's a cleanable tile
				var tile = metaTileMap.GetTile(cellPosition, layerType) as OverlayTile;
				//it's not an overlay tile or it's not cleanable so don't remove it
				if (tile == null || !tile.IsCleanable) continue;
			}

			InternalRemoveTile(cellPosition, layerType);

			SpawnSafeThread.RemoveTileMessageSend(networkMatrix.MatrixSync.netId, cellPosition, layerType);

			AddToChangeList(cellPosition, layerType);
		}
	}

	[Server]
	public void RemoveAllOverlays(Vector3Int cellPosition, LayerType layerType, bool onlyIfCleanable = false)
	{
		cellPosition.z = 0;

		var overlayPos = metaTileMap.GetAllOverlayPos(cellPosition, layerType);
		if(overlayPos == null || overlayPos.Count == 0) return;

		foreach (var overlay in overlayPos)
		{
			cellPosition = overlay;

			if (onlyIfCleanable)
			{
				//only remove it if it's a cleanable tile
				var tile = metaTileMap.GetTile(cellPosition, layerType) as OverlayTile;
				//it's not an overlay tile or it's not cleanable so don't remove it
				if (tile == null || !tile.IsCleanable) continue;
			}

			InternalRemoveTile(cellPosition, layerType);

			SpawnSafeThread.RemoveTileMessageSend(networkMatrix.MatrixSync.netId, cellPosition, layerType);

			AddToChangeList(cellPosition, layerType);
		}
	}

	[Server]
	public void RemoveFloorWallOverlaysOfType(Vector3Int cellPosition, OverlayType overlayType, bool onlyIfCleanable = false)
	{
		RemoveOverlaysOfType(cellPosition, LayerType.Floors, overlayType, onlyIfCleanable);
		RemoveOverlaysOfType(cellPosition, LayerType.Walls, overlayType, onlyIfCleanable);
	}

	[Server]
	public List<OverlayTile> GetAllOverlayTiles(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType)
	{
		return metaTileMap.GetOverlayTilesByType(cellPosition, layerType, overlayType);
	}

	[Server]
	public bool HasOverlay(Vector3Int cellPosition, OverlayTile overlayTile)
	{
		return metaTileMap.HasOverlay(cellPosition, overlayTile.LayerType, overlayTile);
	}

	[Server]
	public bool HasOverlay(Vector3Int cellPosition, TileType tileType, string overlayName)
	{
		var overlayTile = TileManager.GetTile(tileType, overlayName) as OverlayTile;
		if (overlayTile == null) return false;

		return metaTileMap.HasOverlay(cellPosition, overlayTile.LayerType, overlayTile);
	}

	[Server]
	public bool HasOverlayOfType(Vector3Int cellPosition, LayerType layerType, OverlayType overlayType)
	{
		return metaTileMap.HasOverlayOfType(cellPosition, layerType, overlayType);
	}

	/// <summary>
	/// Gets the colour of the first tile of this type
	/// </summary>
	[Server]
	public Color? GetColourOfFirstTile(Vector3Int cellPosition, OverlayType overlayType, LayerType layerType)
	{
		var overlays = metaTileMap.GetOverlayPosByType(cellPosition, layerType, overlayType);
		if (overlays.Count == 0) return null;

		return metaTileMap.GetColour(overlays.First(), layerType);
	}

	#endregion


	[Server]
	public LayerTile GetLayerTile(Vector3Int cellPosition, LayerType layerType)
	{
		return metaTileMap.GetTile(cellPosition, layerType);
	}

	public void InternalRemoveTile(Vector3 position, LayerType layerType)
	{
		Vector3Int p = position.RoundToInt();

		metaTileMap.RemoveTileWithlayer(p, layerType);
	}

	private void AlertClients(Vector3Int position, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		if (color == null)
		{
			color = color.GetValueOrDefault(Color.white);
		}

		if (transformMatrix == null)
		{
			transformMatrix = transformMatrix.GetValueOrDefault(Matrix4x4.identity);
		}

		SpawnSafeThread.UpdateTileMessageSend(networkMatrix.MatrixSync.netId, position, tileType, tileName, (Matrix4x4)transformMatrix, (Color)color);
	}

	public void InternalUpdateTile(Vector3Int position, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);
		metaTileMap.SetTile(position, layerTile, transformMatrix, color);
	}

	public Vector3Int InternalUpdateTile(Vector3 position, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		Vector3Int p = position.RoundToInt();

		return metaTileMap.SetTile(p, layerTile, transformMatrix, color);
	}

	private void AddToChangeList(Vector3Int position, LayerType layerType = LayerType.None,
		TileType tileType = TileType.None, string tileName = null, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = tileType,
			TileName = tileName,
			transformMatrix = transformMatrix,
			color = color
		});
	}

	private void AddToChangeList(Vector3Int position, LayerTile layerTile, LayerType layerType = LayerType.None,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = layerTile.TileType,
			TileName = layerTile.name,
			transformMatrix = transformMatrix,
			color = color
		});
	}

	private bool IsDifferent(Vector3Int position, TileType tileType, string tileName, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);

		return IsDifferent(position, layerTile, transformMatrix, color);
	}

	private bool IsDifferent(Vector3Int position, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		return metaTileMap.IsDifferent(position, layerTile, layerTile.LayerType, transformMatrix, color);
	}

}

[System.Serializable]
public class TileChangeList
{
	public List<TileChangeEntry> List = new List<TileChangeEntry>();

	public static TileChangeList FromList(IEnumerable<TileChangeEntry> entry)
	{
		return new TileChangeList()
		{
			List = entry.ToList()
		};
	}
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector3Int Position;

	public TileType TileType;

	public LayerType LayerType;

	public string TileName;

	public Matrix4x4? transformMatrix;

	public Vector4? color;


}

public class TileChangeEvent : UnityEvent<Vector3Int, GenericTile>
{
}