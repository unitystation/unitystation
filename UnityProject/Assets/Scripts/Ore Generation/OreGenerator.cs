using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.Serialization;

/// <summary>
/// Component which should go on a Matrix and which generates ore tiles in any mineable tiles of that matrix.
/// </summary>
public class OreGenerator : MonoBehaviour
{
	private static readonly List<Vector3Int> DIRECTIONS = new List<Vector3Int>() {
		Vector3Int.up,
		Vector3Int.down,
		Vector3Int.right,
		Vector3Int.left,
		Vector3Int.up + Vector3Int.right,
		Vector3Int.up + Vector3Int.left,
		Vector3Int.down + Vector3Int.right,
		Vector3Int.down + Vector3Int.left
	};

	[FormerlySerializedAs("Data")] [SerializeField]
	private OreGeneratorConfig config = null;


	private static readonly System.Random RANDOM = new System.Random();

	private Tilemap wallTilemap;
	private TileChangeManager tileChangeManager;

	public bool runOnStart = true;

	// Start is called before the first frame update
	void Start()
	{
		if(!runOnStart) return;
		RunOreGenerator();
	}

	public void RunOreGenerator()
	{
		var metaTileMap = GetComponentInChildren<MetaTileMap>();
		wallTilemap = metaTileMap.Layers[LayerType.Walls].GetComponent<Tilemap>();
		tileChangeManager = GetComponent<TileChangeManager>();

		if (CustomNetworkManager.IsServer)
		{
			List<OreProbability> weightedList = new List<OreProbability>();
			foreach (var ores in config.OreProbabilities) {
				for (int i = 0; i < ores.SpawnChance; i++)
				{
					weightedList.Add(ores);
				}
			}

			BoundsInt bounds = wallTilemap.cellBounds;
			List<Vector3Int> miningTiles = new List<Vector3Int>();

			for (int n = bounds.xMin; n < bounds.xMax; n++)
			{
				for (int p = bounds.yMin; p < bounds.yMax; p++)
				{
					Vector3Int localPlace = (new Vector3Int(n, p, 0));

					if (wallTilemap.HasTile(localPlace))
					{
						var tile = wallTilemap.GetTile(localPlace);
						if (tile.name.Contains("rock_wall"))
						{
							miningTiles.Add(localPlace);
						}
					}
				}
			}

			int numberOfTiles = (int)((miningTiles.Count / 100f) * config.Density);
			for (int i = 0; i < numberOfTiles; i++)
			{
				var oreTile = miningTiles[RANDOM.Next(miningTiles.Count)];
				var oreCategory = weightedList[RANDOM.Next(weightedList.Count)];
				tileChangeManager.UpdateTile(oreTile, oreCategory.WallTile);
				var intLocation = oreTile + Vector3Int.zero;
				intLocation.z = -1;
				tileChangeManager.UpdateTile(intLocation, oreCategory.OverlayTile);

				NodeScatter(oreTile, oreCategory);
			}
		}
	}

	private void NodeScatter(Vector3Int location, OreProbability materialSpecified)
	{
		var locations = new List<Vector3Int>() {
			location,
		};
		var strength = materialSpecified.PossibleClusterSizes[RANDOM.Next(materialSpecified.PossibleClusterSizes.Count)];
		while (strength > 0)
		{
			var chosenLocation = locations[RANDOM.Next(locations.Count)];
			var ranLocation = chosenLocation + DIRECTIONS[RANDOM.Next(DIRECTIONS.Count)];
			var tile = wallTilemap.GetTile(ranLocation);
			if (tile != null && tile.name.Contains("rock_wall"))
			{
				tileChangeManager.UpdateTile(ranLocation, materialSpecified.WallTile);
				locations.Add(ranLocation);
				ranLocation.z = -1;
				tileChangeManager.UpdateTile(ranLocation, materialSpecified.OverlayTile);
			}
			strength--;
		}
	}
}

/// <summary>
/// Defines the probability logic of generating a given type of ore
/// </summary>
[Serializable]
public class OreProbability
{
	[Tooltip("Wall tile to use for this ore tile")]
	[FormerlySerializedAs("Tile")]
	public LayerTile WallTile;

	[Tooltip("Overlay (Effects layer) tile to use for this ore tile")]
	public LayerTile OverlayTile;

	[Tooltip("How likely this ore is to spawn compared to the others in the list. Think of each entry in the  as" +
	         " being a chit in a bag. This defines the number of chits to add representing this tile.")]
	[FormerlySerializedAs("BlockWeight")]
	public int SpawnChance;


	[Tooltip("Possible sizes of clusters this ore can spawn. An entry is randomly chosen from this list when" +
	         " an ore cluster of this type is spawned, and the value determines roughly the number of ore tiles that will" +
	         " spawn in this cluster.")]
	[FormerlySerializedAs("NumberBlocks")] public List<int> PossibleClusterSizes = new List<int>();

}
