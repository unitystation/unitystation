using System.Linq;
using UnityEngine;
using Objects.Construction;
using Chemistry;
using Chemistry.Components;

public static class EffectsFactory
{
	private static GameObject smallBloodTile;
	private static GameObject mediumBloodTile;
	private static GameObject largeBloodTile;
	private static GameObject waterTile;
	private static GameObject chemTile;
	private static GameObject powderTile;

	private static GameObject smallXenoBloodTile;
	private static GameObject medXenoBloodTile;
	private static GameObject largeXenoBloodTile;


	private static void EnsureInit()
	{
		if (smallBloodTile == null)
		{
			//Do init stuff
			//TODO: Make only ONE bloodTile prefab that can handel all sizes.
			smallBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("SmallBloodSplat");
			mediumBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("MediumBloodSplat");
			largeBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("LargeBloodSplat");
			waterTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("WaterSplat");
			chemTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("ChemSplat");
			powderTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("PowderSplat");
			smallXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("SmallXenoBloodSplat");
			medXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("MedXenoBloodSplat");
			largeXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("LargeXenoBloodSplat");
		}
	}

	public static void BloodSplat(Vector3 worldPos, ReagentMix bloodReagents = default, BloodSplatSize splatSize = default, BloodSplatType bloodColorType = default)
	{
		EnsureInit();

		var chosenTile = new GameObject();

		if (bloodReagents.Total < 0.1f)
		{
			chosenTile = smallBloodTile;
		}
		else if(bloodReagents.Total > 0.1f && bloodReagents.Total < 0.3f)
		{
			chosenTile = mediumBloodTile;
		}
		else
		{
			chosenTile = largeBloodTile;
		}

		var bloodTileInst = Spawn.ServerPrefab(chosenTile, worldPos, MatrixManager.AtPoint(worldPos.CutToInt(), true).Objects, Quaternion.identity);
		if (bloodTileInst.Successful)
		{
			var bloodTileGO = bloodTileInst.GameObject;
			var tileReagents = bloodTileGO.GetComponent<ReagentContainer>();
			if (bloodTileGO)
			{
				var decal = bloodTileGO.GetComponent<FloorDecal>();
				if (decal)
				{
					decal.color = bloodReagents.MixColor;
					tileReagents.Add(bloodReagents);
				}
			}
		}
	}

	public static void WaterSplat(Vector3Int worldPos)
	{
		if (MatrixManager.IsSpaceAt(worldPos, true))
		{
			return;
		}
		//don't do multiple splats
		if (MatrixManager.GetAt<FloorDecal>(worldPos, isServer: true).Any(decal => decal.CanDryUp))
		{
			return;
		}
		EnsureInit();
		Spawn.ServerPrefab(waterTile, worldPos,	MatrixManager.AtPoint(worldPos, true).Objects, Quaternion.identity);
	}

	public static void ChemSplat(Vector3Int worldPos, Color color, ReagentMix reagents)
	{
		EnsureInit();
		var chemTileInst = Spawn.ServerPrefab(chemTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects, Quaternion.identity);
		if (chemTileInst.Successful)
		{
			var chemTileGO = chemTileInst.GameObject;
			var tileReagents = chemTileGO.GetComponent<ReagentContainer>();
			if (chemTileGO)
			{
				var decal = chemTileGO.GetComponent<FloorDecal>();
				if (decal)
				{
					decal.color = color;
				}
				if (reagents != null)
				{
					tileReagents.Add(reagents);
				}
			}
		}
	}

	public static void PowderSplat(Vector3Int worldPos, Color color, ReagentMix reagents)
	{
		EnsureInit();
		var powderTileInst = Spawn.ServerPrefab(powderTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects, Quaternion.identity);
		if (powderTileInst.Successful)
		{
			var powderTileGO = powderTileInst.GameObject;
			var tileReagents = powderTileGO.GetComponent<ReagentContainer>();
			if (powderTileGO)
			{
				var decal = powderTileGO.GetComponent<FloorDecal>();
				if (decal)
				{
					decal.color = color;
				}
				if (reagents != null)
				{
					tileReagents.Add(reagents);
				}
			}
		}
	}
}
