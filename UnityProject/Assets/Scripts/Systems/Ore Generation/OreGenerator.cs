﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using Initialisation;
using Mirror;
using Shuttles;
using TileManagement;
using TileMap.Behaviours;
using Tilemaps.Behaviours.Layers;
using UnityEngine.Serialization;
using Tiles;

/// <summary>
/// Component which should go on a Matrix and which generates ore tiles in any mineable tiles of that matrix.
/// </summary>
public class OreGenerator : ItemMatrixSystemInit
{
	private static readonly List<Vector3Int> DIRECTIONS = new List<Vector3Int>()
	{
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

	public override int Priority => priority;

	public int priority = 98;
	private static readonly System.Random RANDOM = new System.Random();

	private Tilemap wallTilemap;
	private HashSet<Vector3> GeneratedLocations = new HashSet<Vector3>();

	public bool RunAutomatic = true;

	public override void Initialize()
	{
		if (RunAutomatic)
		{
			RunOreGenerator();
		}
	}

	public void RunOreGenerator()
	{
		if (isServer == false) return;
		GeneratedLocations.Clear();
		wallTilemap = MetaTileMap.Layers[LayerType.Walls].GetComponent<Tilemap>();

		if (NetworkedMatrix.MatrixSync == null)
		{
			NetworkedMatrix.BackUpSetMatrixSync();

			if (NetworkedMatrix.MatrixSync.netId == 0)
			{
				StartCoroutine(WaitForNetId(NetworkedMatrix.MatrixSync));
				return;
			}
		}

		List<OreProbability> weightedList = new List<OreProbability>();
		foreach (var ores in config.OreProbabilities)
		{
			for (int i = 0; i < ores.SpawnChance; i++)
			{
				weightedList.Add(ores);
			}
		}

		//TODO move BoundsInt bounds = wallTilemap.cellBounds to metaTileMap
		BetterBoundsInt bounds = MetaTileMap.GetLocalBounds();
		List<Vector3Int> miningTiles = new List<Vector3Int>();

		for (int n = bounds.xMin; n < bounds.xMax; n++)
		{
			for (int p = bounds.yMin; p < bounds.yMax; p++)
			{
				Vector3Int localPlace = (new Vector3Int(n, p, 0));

				if (MetaTileMap.HasTile(localPlace))
				{
					BasicTile tile = MetaTileMap.GetTile(localPlace, LayerType.Walls) as BasicTile;
					if (tile != null && tile.Mineable) miningTiles.Add(localPlace);
				}
			}
		}

		int numberOfTiles = (int) ((miningTiles.Count / 100f) * config.Density);

		for (int i = 0; i < numberOfTiles; i++)
		{
			var oreTile = miningTiles[RANDOM.Next(miningTiles.Count)];
			if (GeneratedLocations.Contains(oreTile) == false)
			{
				GeneratedLocations.Add(oreTile);
				var oreCategory = weightedList[RANDOM.Next(weightedList.Count)];
				tileChangeManager.MetaTileMap.SetTile(oreTile, oreCategory.WallTile, MapSaveRecord: true);
				var intLocation = oreTile + Vector3Int.zero;
				intLocation.z = -1;
				tileChangeManager.MetaTileMap.AddOverlay(intLocation, oreCategory.OverlayTile as OverlayTile);
				NodeScatter(oreTile, oreCategory);
			}
		}
	}

	private void NodeScatter(Vector3Int location, OreProbability materialSpecified)
	{
		var locations = new List<Vector3Int>()
		{
			location,
		};
		var strength =
			materialSpecified.PossibleClusterSizes[RANDOM.Next(materialSpecified.PossibleClusterSizes.Count)];
		while (strength > 0)
		{
			var chosenLocation = locations[RANDOM.Next(locations.Count)];
			var ranLocation = chosenLocation + DIRECTIONS[RANDOM.Next(DIRECTIONS.Count)];
			var tile = MetaTileMap.GetTile(ranLocation, ignoreEffectsLayer: true);

			if (tile != null && ((BasicTile) tile).Mineable && GeneratedLocations.Contains(ranLocation) == false)
			{
				GeneratedLocations.Add(ranLocation);
				tileChangeManager.MetaTileMap.SetTile(ranLocation, materialSpecified.WallTile, MapSaveRecord : true);
				locations.Add(ranLocation);
				ranLocation.z = -1;
				tileChangeManager.MetaTileMap.AddOverlay(ranLocation, materialSpecified.OverlayTile as OverlayTile);
			}


			strength--;
		}
	}

	private IEnumerator WaitForNetId(MatrixSync matrixSync)
	{
		while (matrixSync.netId == 0)
		{
			yield return WaitFor.EndOfFrame;
		}

		RunOreGenerator();
	}
}

/// <summary>
/// Defines the probability logic of generating a given type of ore
/// </summary>
[Serializable]
public class OreProbability
{
	[Tooltip("Wall tile to use for this ore tile")] [FormerlySerializedAs("Tile")]
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
	[FormerlySerializedAs("NumberBlocks")]
	public List<int> PossibleClusterSizes = new List<int>();
}