using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class OreGenerator : MonoBehaviour
{
	List<WeightNStrength> WeightedList = new List<WeightNStrength>();

	List<Vector3Int> Directions = new List<Vector3Int>() {
		Vector3Int.up,
		Vector3Int.down,
		Vector3Int.right,
		Vector3Int.left,
		Vector3Int.up + Vector3Int.right,
		Vector3Int.up + Vector3Int.left,
		Vector3Int.down + Vector3Int.right,
		Vector3Int.down + Vector3Int.left
	};

	public OreGeneratorScriptableObject Data;


	public static System.Random random = new System.Random();

	public Tilemap WallTilemap;
	public Matrix Matrix;

	public TileChangeManager TileChangeManager;
	// Start is called before the first frame update
	void Start()
	{
		if (CustomNetworkManager.Instance._isServer != false)
		{
			foreach (var Ores in Data.FullList) { 
				AddElementList(Ores);
			}

			BoundsInt bounds = WallTilemap.cellBounds;
			Logger.Log(bounds.ToString());
			List<TileAndLocation> MiningTiles = new List<TileAndLocation>();
			//Logger.Log(Matrix.CompressAllBounds() cellBounds.ToString());


			for (int n = bounds.xMin; n < bounds.xMax; n++)
			{
				for (int p = bounds.yMin; p < bounds.yMax; p++)
				{
					Vector3Int localPlace = (new Vector3Int(n, p, 0));

					if (WallTilemap.HasTile(localPlace))
					{
						MiningTiles.Add(new TileAndLocation(WallTilemap.GetTile(localPlace) as BasicTile, localPlace));
					}
				}
			}

			//if (allTiles != null)
			//{
			//	foreach (var _Tile in allTiles)
			//	{
			//		if (_Tile != null && (_Tile is BasicTile))
			//		{
			//			var basicWallTile = _Tile as BasicTile;
			//			if (basicWallTile.Mineable)
			//			{
			//				Logger.Log(basicWallTile.ToString());
			//				MiningTiles.Add(basicWallTile);
			//				//basicWallTile.
			//			}
			//		}
			//	}
			//}
			int NumberOfTiles = (int)((MiningTiles.Count / 100f) * Data.Density);
			for (int i = 0; i < NumberOfTiles; i++)
			{
				var OreTile = MiningTiles[random.Next(MiningTiles.Count)];
				var OreCategorie = WeightedList[random.Next(WeightedList.Count)];
				//case OreCategorie.Bananium:
				//		TileChangeManager.UpdateTile(OreTile.Location, Data.Bananium);
				//;
				//break;
				TileChangeManager.UpdateTile(OreTile.Location, OreCategorie.Tile);
				var intLocation = OreTile.Location + Vector3Int.zero;
				intLocation.z = -1;
				TileChangeManager.UpdateTile(intLocation, OreCategorie.OverlayTile);

				NodeScatter(OreTile.Location, OreCategorie);
			}
		}
	}

	void NodeScatter(Vector3Int Location, WeightNStrength MaterialSpecified)
	{


		var Locations = new List<Vector3Int>() {
			Location,
		};
		var Strength = MaterialSpecified.NumberBlocks[random.Next(MaterialSpecified.NumberBlocks.Count)];
		while (Strength > 0)
		{
			var ChosenLocation = Locations[random.Next(Locations.Count)];
			var ranLocation = Location + Directions[random.Next(Directions.Count)];
			if (WallTilemap.GetTile(ranLocation) != null)
			{
				TileChangeManager.UpdateTile(ranLocation, MaterialSpecified.Tile);
				Locations.Add(ranLocation);
				ranLocation.z = -1;
				TileChangeManager.UpdateTile(ranLocation, MaterialSpecified.OverlayTile);
			}
			Strength--;
		}
	}

	void AddElementList(WeightNStrength num)
	{
		for (int i = 0; i < num.BlockWeight; i++)
		{
			WeightedList.Add(num);
		}
	}
}


public struct TileAndLocation
{
	public TileAndLocation(BasicTile _Tile, Vector3Int _Location)
	{
		Tile = _Tile;
		Location = _Location;
	}
	public BasicTile Tile;	public Vector3Int Location;
}

[Serializable]
public class WeightNStrength
{
	public LayerTile Tile;
	public LayerTile OverlayTile;

	public int BlockWeight;
	public int BlockStrength;
	public List<int> NumberBlocks = new List<int>();

}


public enum OreCategorie
{
	None,
	Iron,
	Plasma,
	Silver,
	Gold,
	Uranium,
	BlueSpace,
	Titanium,
	Diamond,
	Bananium
}