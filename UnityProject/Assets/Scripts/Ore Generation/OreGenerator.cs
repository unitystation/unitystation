using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class OreGenerator : MonoBehaviour
{
	List<OreCategorie> WeightedList = new List<OreCategorie>();

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

			AddElementList(OreCategorie.Iron, Data.IronBlockWeight);
			AddElementList(OreCategorie.Plasma, Data.PlasmaBlockWeight);
			AddElementList(OreCategorie.Silver, Data.SilverBlockWeight);
			AddElementList(OreCategorie.Gold, Data.GoldBlockWeight);
			AddElementList(OreCategorie.Uranium, Data.UraniumBlockWeight);
			AddElementList(OreCategorie.BlueSpace, Data.BlueSpaceBlockWeight);
			AddElementList(OreCategorie.Titanium, Data.TitaniumBlockWeight);
			AddElementList(OreCategorie.Diamond, Data.DiamondBlockWeight);
			AddElementList(OreCategorie.Bananium, Data.BananiumBlockWeight);

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
				switch (OreCategorie)
				{
					case OreCategorie.Iron:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Iron);
						NodeScatter(OreTile.Location, Data.IronBlockWeight, Data.Iron);
						break;
					case OreCategorie.Plasma:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Plasma);
						NodeScatter(OreTile.Location, Data.PlasmaBlockWeight, Data.Plasma);
						break;
					case OreCategorie.Silver:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Silver);
						NodeScatter(OreTile.Location, Data.SilverBlockWeight, Data.Silver);

						break;
					case OreCategorie.Gold:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Gold);
						NodeScatter(OreTile.Location, Data.GoldBlockWeight, Data.Gold);
						break;
					case OreCategorie.Uranium:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Uranium);
						NodeScatter(OreTile.Location, Data.UraniumBlockWeight, Data.Uranium);
						break;

					case OreCategorie.BlueSpace:
						TileChangeManager.UpdateTile(OreTile.Location, Data.BlueSpace);
						NodeScatter(OreTile.Location, Data.BlueSpaceBlockWeight, Data.BlueSpace);
						break;

					case OreCategorie.Titanium:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Titanium);
						NodeScatter(OreTile.Location, Data.TitaniumBlockWeight, Data.Titanium);
						break;

					case OreCategorie.Diamond:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Diamond);
						NodeScatter(OreTile.Location, Data.TitaniumBlockWeight, Data.Diamond);
						break;
					case OreCategorie.Bananium:
						TileChangeManager.UpdateTile(OreTile.Location, Data.Bananium);
						NodeScatter(OreTile.Location, Data.BananiumBlockWeight, Data.Bananium);
						break;
				}
			}
		}
	}

	void NodeScatter(Vector3Int Location, WeightNStrength InWeightNStrength, LayerTile MaterialSpecified)
	{


		var Locations = new List<Vector3Int>() {
			Location,
		};
		var Strength = InWeightNStrength.NumberBlocks[random.Next(InWeightNStrength.NumberBlocks.Count)];
		while (Strength > 0)
		{
			var ChosenLocation = Locations[random.Next(Locations.Count)];
			var ranLocation = Location + Directions[random.Next(Directions.Count)];
			if (WallTilemap.GetTile(ranLocation) != null)
			{
				TileChangeManager.UpdateTile(ranLocation, MaterialSpecified);
				Locations.Add(ranLocation);
			}
			Strength--;
		}




	}

	void AddElementList(OreCategorie Ore, WeightNStrength num)
	{
		for (int i = 0; i < num.BlockWeight; i++)
		{
			WeightedList.Add(Ore);
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