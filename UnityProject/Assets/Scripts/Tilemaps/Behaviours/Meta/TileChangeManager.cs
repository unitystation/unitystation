using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class TileChangeManager : NetworkBehaviour
{
	private MetaTileMap metaTileMap;

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
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		TileChangeNewPlayer.Send(netId);
	}

	public void InitServerSync(string data)
	{
		//server doesn't ever need to run this because this will replay its own changes
		if (CustomNetworkManager.IsServer) return;

		//Unpacking the data example (and then run action change)
		var dataList = JsonUtility.FromJson<TileChangeList>(data);
		foreach (TileChangeEntry entry in dataList.List)
		{
			Logger.LogTraceFormat("Received update for {0} layer {1} " + entry.TileName, Category.TileMaps,
				entry.Position,
				entry.LayerType);
			// load tile & apply
			if (entry.TileType.Equals(TileType.None))
			{
				InternalRemoveTile(entry.Position, entry.LayerType, entry.RemoveAll);
			}
			else
			{
				InternalUpdateTile(entry.Position, entry.TileType, entry.TileName,entry.transformMatrix, entry.color );
			}
		}
	}

	[Server]
	public void UpdateNewPlayer(NetworkConnection requestedBy)
	{
		if (changeList.List.Count > 0)
		{
			TileChangesNewClientSync.Send(gameObject, requestedBy, changeList);
		}
	}

	[Server]
	public void UpdateTile(Vector3Int cellPosition, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		if (IsDifferent(cellPosition, tileType, tileName, transformMatrix, color))
		{
			InternalUpdateTile(cellPosition, tileType, tileName, transformMatrix, color);

			ChooseCorrectRpc(cellPosition, tileType, tileName, transformMatrix, color);

			AddToChangeList(cellPosition, tileType: tileType, tileName: tileName, transformMatrix : transformMatrix , color: color);
		}
	}


	[Server]
	public void UpdateTile(Vector3Int cellPosition, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		if (IsDifferent(cellPosition, layerTile, transformMatrix, color))
		{
			InternalUpdateTile(cellPosition, layerTile,transformMatrix, color);

			ChooseCorrectRpc(cellPosition, layerTile.TileType, layerTile.name, transformMatrix, color);

			AddToChangeList(cellPosition, layerTile, transformMatrix : transformMatrix , color: color);
		}
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
		ChooseCorrectRpc(cellPosition, layerTile.TileType, layerTile.name, transformMatrix, color);
		AddToChangeList(cellPosition, layerTile, transformMatrix: transformMatrix, color : color);
	}

	/// <summary>
	/// Like UpdateTile, but operates on z=-1 of the affected layer.
	/// Adds overlay tile as an overlay at z=-1 of the layer that overlayTile is configured for.
	/// No effect if there is no tile at z=0 of the indicated position (there is nothing
	/// to overlay on top of).
	/// </summary>
	/// <param name="cellPosition"></param>
	/// <param name="tileName"></param>
	[Server]
	public void UpdateOverlay(Vector3Int cellPosition, OverlayTile overlayTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		cellPosition.z = 0;
		if (!metaTileMap.HasTile(cellPosition, overlayTile.LayerType, true)) return;
		cellPosition.z = -1;
		if (IsDifferent(cellPosition, overlayTile))
		{
			InternalUpdateTile(cellPosition, overlayTile, transformMatrix, color);

			ChooseCorrectRpc(cellPosition, overlayTile.TileType, overlayTile.name, transformMatrix, color);

			AddToChangeList(cellPosition, overlayTile, transformMatrix : transformMatrix,color : color );
		}
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
	/// <param name="removeAll"></param>
	/// <returns></returns>
	[Server]
	public LayerTile RemoveTile(Vector3Int cellPosition, LayerType layerType, bool removeAll = true)
	{
		var layerTile = metaTileMap.GetTile(cellPosition, layerType);
		if (metaTileMap.HasTile(cellPosition, layerType, true))
		{
			InternalRemoveTile(cellPosition, layerType, removeAll);

			RpcRemoveTile(cellPosition, layerType, removeAll);

			AddToChangeList(cellPosition, layerType);

			if (layerType == LayerType.Floors || layerType == LayerType.Base)
			{
				OnFloorOrPlatingRemoved.Invoke(cellPosition);
			}
			else if (layerType == LayerType.Windows)
			{
				RemoveTile(cellPosition, LayerType.Effects);
			}

			return layerTile;
		}

		return layerTile;
	}

	[Server]
	public void RemoveOverlay(Vector3Int cellPosition, LayerType layerType, bool onlyIfCleanable = false)
	{
		cellPosition.z = -1;

		if (metaTileMap.HasTile(cellPosition, layerType, true))
		{
			if (onlyIfCleanable)
			{
				//only remove it if it's a cleanable tile
				var tile = metaTileMap.GetTile(cellPosition, layerType) as OverlayTile;
				//it's not an overlay tile or it's not cleanable so don't remove it
				if (tile == null || !tile.IsCleanable) return;
			}

			InternalRemoveTile(cellPosition, layerType, false);

			RpcRemoveTile(cellPosition, layerType, false);

			AddToChangeList(cellPosition, layerType, removeAll: true);
		}
	}

	[Server]
	public LayerTile GetLayerTile(Vector3Int cellPosition, LayerType layerType)
	{
		return metaTileMap.GetTile(cellPosition, layerType);
	}

	[ClientRpc]
	private void RpcRemoveTile(Vector3 position, LayerType layerType, bool removeAll)
	{
		if (isServer)
		{
			return;
		}

		InternalRemoveTile(position, layerType, removeAll);
	}

	private void InternalRemoveTile(Vector3 position, LayerType layerType, bool removeAll)
	{
		Vector3Int p = position.RoundToInt();

		metaTileMap.RemoveTile(p, layerType, removeAll);
	}

	private void ChooseCorrectRpc(Vector3Int position, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		if (transformMatrix != null)
		{
			if (color != null)
			{
				RpcUpdateTileMC(position, tileType, tileName, transformMatrix.GetValueOrDefault(Matrix4x4.identity),
					color.GetValueOrDefault(Color.white));
				return;
			}

			RpcUpdateTileM(position, tileType, tileName, transformMatrix.GetValueOrDefault(Matrix4x4.identity));
			return;

		}

		if (color != null) { RpcUpdateTileC(position, tileType, tileName, color.GetValueOrDefault(Color.white)); return;
		}


		RpcUpdateTile(position, tileType, tileName);
		return;
	}

	[ClientRpc]
	private void RpcUpdateTileMC(Vector3Int position, TileType tileType, string tileName, Matrix4x4 transformMatrix,
		Color color)
	{
		if (isServer)
		{
			return;
		}

		InternalUpdateTile(position, tileType, tileName, transformMatrix, color);
	}

	[ClientRpc]
	private void RpcUpdateTileM(Vector3Int position, TileType tileType, string tileName, Matrix4x4 transformMatrix)
	{
		if (isServer)
		{
			return;
		}

		InternalUpdateTile(position, tileType, tileName, transformMatrix: transformMatrix);
	}

	[ClientRpc]
	private void RpcUpdateTileC(Vector3Int position, TileType tileType, string tileName, Color color)
	{
		if (isServer)
		{
			return;
		}

		InternalUpdateTile(position, tileType, tileName, color: color);
	}

	[ClientRpc]
	private void RpcUpdateTile(Vector3Int position, TileType tileType, string tileName)
	{
		if (isServer)
		{
			return;
		}

		InternalUpdateTile(position, tileType, tileName);
	}


	private void InternalUpdateTile(Vector3Int position, TileType tileType, string tileName,
		Matrix4x4? transformMatrix = null, Color? color = null)
	{
		LayerTile layerTile = TileManager.GetTile(tileType, tileName);
		metaTileMap.SetTile(position, layerTile, transformMatrix, color);
		//if we are changing a tile at z=0, make sure to remove any overlays it has as well

		//TODO: OVERLAYS - right now it only removes at z = -1, but we will eventually need
		//to allow multiple overlays on a given location which would require multiple z levels.
		if (layerTile.TileType != TileType.UnderFloor)
		{
			if (position.z == 0)
			{
				position.z = -1;
				if (metaTileMap.HasTile(position, layerTile.LayerType, true))
				{
					metaTileMap.RemoveTile(position, layerTile.LayerType);
				}
			}
		}
	}

	private void InternalUpdateTile(Vector3 position, LayerTile layerTile, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		if (layerTile.TileType == TileType.WindowDamaged)
		{
			position.z -= 1;
		}

		Vector3Int p = position.RoundToInt();

		metaTileMap.SetTile(p, layerTile, transformMatrix, color);

	}

	private void AddToChangeList(Vector3Int position, LayerType layerType = LayerType.None,
		TileType tileType = TileType.None, string tileName = null, bool removeAll = false, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = tileType,
			TileName = tileName,
			RemoveAll = removeAll,
			transformMatrix = transformMatrix,
			color = color
		});
	}

	private void AddToChangeList(Vector3Int position, LayerTile layerTile, LayerType layerType = LayerType.None,
		bool removeAll = false, Matrix4x4? transformMatrix = null,
		Color? color = null)
	{
		changeList.List.Add(new TileChangeEntry()
		{
			Position = position,
			LayerType = layerType,
			TileType = layerTile.TileType,
			TileName = layerTile.name,
			RemoveAll = removeAll,
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

	public bool RemoveAll;

	public Matrix4x4? transformMatrix;

	public Color? color;

}

public class TileChangeEvent : UnityEvent<Vector3Int, GenericTile>
{
}