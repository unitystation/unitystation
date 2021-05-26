using System.Linq;
using UnityEngine;
using Objects.Construction;

public static class EffectsFactory
{
	private static GameObject smallBloodTile;
	private static GameObject mediumBloodTile;
	private static GameObject largeBloodTile;
	private static GameObject waterTile;
	private static GameObject chemTile;

	private static GameObject smallXenoBloodTile;
	private static GameObject medXenoBloodTile;
	private static GameObject largeXenoBloodTile;

	private static void EnsureInit()
	{
		if (smallBloodTile == null)
		{
			//Do init stuff
			smallBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("SmallBloodSplat");
			mediumBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("MediumBloodSplat");
			largeBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("LargeBloodSplat");
			waterTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("WaterSplat");
			chemTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("ChemSplat");
			smallXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("SmallXenoBloodSplat");
			medXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("MedXenoBloodSplat");
			largeXenoBloodTile = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("LargeXenoBloodSplat");
		}
	}

	public static void BloodSplat(Vector3 worldPos, BloodSplatSize splatSize, BloodSplatType bloodColorType)
	{
		EnsureInit();
		GameObject chosenTile = null;
		switch (bloodColorType)
		{
			case BloodSplatType.red:
				switch (splatSize)
				{
					case BloodSplatSize.small:
						chosenTile = smallBloodTile;
						break;
					case BloodSplatSize.medium:
						chosenTile = mediumBloodTile;
						break;
					case BloodSplatSize.large:
						chosenTile = largeBloodTile;
						break;
					case BloodSplatSize.Random:
						int rand = Random.Range(0, 3);
						BloodSplat(worldPos, (BloodSplatSize)rand, bloodColorType);
						return;
				}
				break;
			case BloodSplatType.green:
				switch (splatSize)
				{
					case BloodSplatSize.small:
						chosenTile = smallXenoBloodTile;
						break;
					case BloodSplatSize.medium:
						chosenTile = medXenoBloodTile;
						break;
					case BloodSplatSize.large:
						chosenTile = largeXenoBloodTile;
						break;
					case BloodSplatSize.Random:
						int rand = Random.Range(0, 3);
						BloodSplat(worldPos, (BloodSplatSize)rand, bloodColorType);
						return;
				}
				break;
			case BloodSplatType.none:
						return;

		}

		if (chosenTile != null)
		{
			var matrix = MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), true);
			if (matrix.Matrix.Get<FloorDecal>(worldPos.ToLocalInt(matrix.Matrix), true).Count() == 0)
			{
				Spawn.ServerPrefab(chosenTile, worldPos,
								   matrix.Objects);
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

	public static void ChemSplat(Vector3Int worldPos, Color color)
	{
		EnsureInit();
		var chemTileInst = Spawn.ServerPrefab(chemTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects, Quaternion.identity);
		if (chemTileInst.Successful)
		{
			var chemTileGO = chemTileInst.GameObject;
			if (chemTileGO)
			{
				var decal = chemTileGO.GetComponent<FloorDecal>();
				if (decal)
				{
					decal.color = color;
				}
			}
		}
	}
}
