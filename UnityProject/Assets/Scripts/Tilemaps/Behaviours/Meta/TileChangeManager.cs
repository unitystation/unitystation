using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

//To track tile changes on the server and update on the client
public class TileChangeManager : NetworkBehaviour
{
	public Tilemap floorTileMap { get; private set; }
	public Tilemap baseTileMap { get; private set; }
	public Tilemap wallTileMap { get; private set; }
	public Tilemap windowTileMap { get; private set; }
	public Tilemap objectTileMap { get; private set; }
	public Tilemap grillTileMap { get; private set; }

	public GameObject ObjectParent => objectTileMap.gameObject;

	private TileChangeRegister ChangeRegister = new TileChangeRegister();

	void Start()
	{
		CacheTileMaps();
	}

	[Server]
	public void NotifyPlayer(GameObject requestedBy)
	{
		Logger.LogWarning("We do not need to sync tilemap change data anymore but the system is in place " +
			"to preform database state backups", Category.TileMaps);

		//Example of converting all the tile change data into json which can be stored as a blob in a database:
		// var jsondata = JsonUtility.ToJson (ChangeRegister);
		// TileChangesNewClientSync.Send (gameObject, requestedBy, jsondata);
	}

	public void InitServerSync(string data)
	{
		//Unpacking the data example (and then run action change)
		var newChangeRegister = JsonUtility.FromJson<TileChangeRegister>(data);
	}

	void CacheTileMaps()
	{
		var tilemaps = GetComponentsInChildren<Tilemap>(true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains("Floors"))
			{
				floorTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Base"))
			{
				baseTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Walls"))
			{
				wallTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Windows"))
			{
				windowTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Objects"))
			{
				objectTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Grill"))
			{
				grillTileMap = tilemaps[i];
			}
		}
	}

	[Server]
	public void ChangeTile(string newTileName, Vector3Int tileCellPos, TileChangeLayer layer)
	{

		ActionChange(new TileChangeEntry
		{
			cellPosition = tileCellPos,
				tileKey = newTileName,
				layerToChange = layer
		});
	}

	///<Summary>
	/// Please use RemoteTile instead of ChangeTile with null tilekey as additional clean up is preformed in this method
	///</Summary>
	[Server]
	public void RemoveTile(Vector3Int tileCellPos, TileChangeLayer layer)
	{
		ActionChange(new TileChangeEntry
		{
			cellPosition = tileCellPos,
				tileKey = "",
				layerToChange = layer
		});

		if (layer == TileChangeLayer.Window)
		{
			//Clear the crack effects if they exist:
			ActionChange(new TileChangeEntry
			{
				cellPosition = tileCellPos,
					tileKey = "",
					layerToChange = TileChangeLayer.WindowDamage
			});
		}
	}

	private void ActionChange(TileChangeEntry changeEntry)
	{
		if (!TilesManager.ValidateTileKey(changeEntry.layerToChange, changeEntry.tileKey))
		{
			Logger.LogError("No key found for Tile Change", Category.TileMaps);
			return;
		}

		//Set the correct z Level for the tile
		changeEntry.cellPosition.z = GetLayerZLevel(changeEntry.layerToChange);

		var tileMap = GetTilemap(changeEntry.layerToChange);
		if (tileMap == null)
		{
			return;
		}
		TileBase newTile = TilesManager.GetTile(changeEntry.layerToChange, changeEntry.tileKey);
		tileMap.SetTile(Vector3Int.RoundToInt(changeEntry.cellPosition), newTile);

		if (isServer)
		{
			RpcActionChange(changeEntry.cellPosition, changeEntry.tileKey, changeEntry.layerToChange);
		}

		//Store the change data and later we will store it in a database (so when a server crashes it is safe or to replay station change events)
		ChangeRegister.allEntries.Add(changeEntry);
	}

	[ClientRpc]
	private void RpcActionChange(Vector3 cellPos, string tileKey, TileChangeLayer layer)
	{
		ActionChange(new TileChangeEntry
		{
			cellPosition = Vector3Int.RoundToInt(cellPos),
				tileKey = tileKey,
				layerToChange = layer
		});
	}

	public Tilemap GetTilemap(TileChangeLayer layer)
	{
		switch (layer)
		{
			case TileChangeLayer.Base:
				return baseTileMap;
			case TileChangeLayer.Floor:
				return floorTileMap;
			case TileChangeLayer.Object:
				return objectTileMap;
			case TileChangeLayer.Wall:
				return wallTileMap;
			case TileChangeLayer.Window:
			case TileChangeLayer.WindowDamage:
				return windowTileMap;
			case TileChangeLayer.BrokenGrill: //Broken grills go over floors so there is no collider on the grills layer
				return floorTileMap;
			case TileChangeLayer.Grill:
				return grillTileMap;
		}
		return null;
	}

	public Tilemap GetTilemap(LayerType layer)
	{
		switch (layer)
		{
			case LayerType.Base:
				return baseTileMap;
			case LayerType.Floors:
				return floorTileMap;
			case LayerType.Objects:
				return objectTileMap;
			case LayerType.Walls:
				return wallTileMap;
			case LayerType.Windows:
				return windowTileMap;
			case LayerType.Grills:
				return grillTileMap;
		}
		return null;
	}

	private int GetLayerZLevel(TileChangeLayer layer)
	{
		switch (layer)
		{
			case TileChangeLayer.Base:
				return 0;
			case TileChangeLayer.Floor:
				return 0;
			case TileChangeLayer.Object:
				return 0;
			case TileChangeLayer.Wall:
				return 0;
			case TileChangeLayer.Window:
				return 0;
			case TileChangeLayer.WindowDamage:
				return -1;
			case TileChangeLayer.BrokenGrill:
				return -10;
		}
		return 0;
	}
}

[System.Serializable]
public class TileChangeRegister
{
	public List<TileChangeEntry> allEntries = new List<TileChangeEntry>();
}

[System.Serializable]
public class TileChangeEntry
{
	public Vector3Int cellPosition;
	///<Summary>
	/// Set tileKey to empty string to deconstruct
	///</Summary>
	public string tileKey;

	public TileChangeLayer layerToChange;
}

//Remember: That tilemaps can have tile stacked ontop of each other. 
//The lower the number the higher it is in the stack (-10 will be shown ontop of -9)
//Layer Z positions are determined off the TileChangeLayer types below
public enum TileChangeLayer
{
	Floor,
	Base,
	Wall,
	Window,
	Object,
	WindowDamage,
	Grill,
	BrokenGrill, //Damaged grill sprites are placed over a floor tile at position -10 this is because we don't want a collider on this tile on the object layer
}