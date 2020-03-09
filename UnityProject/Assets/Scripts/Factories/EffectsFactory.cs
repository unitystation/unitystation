using System.Linq;
using UnityEngine;

public static class EffectsFactory
{

	private static GameObject fireTile;

	private static GameObject smallBloodTile;
	private static GameObject mediumBloodTile;
	private static GameObject largeBloodTile;
	private static GameObject largeAshTile;
	private static GameObject smallAshTile;
	private static GameObject waterTile;
	private static GameObject chemTile;

	private static GameObject smallXenoBloodTile;
	private static GameObject medXenoBloodTile;
	private static GameObject largeXenoBloodTile;

	private static void EnsureInit()
	{
		if (fireTile == null)
		{
			//Do init stuff
			fireTile = Resources.Load("FireTile") as GameObject;
			smallBloodTile = Resources.Load("SmallBloodSplat") as GameObject;
			mediumBloodTile = Resources.Load("MediumBloodSplat") as GameObject;
			largeBloodTile = Resources.Load("LargeBloodSplat") as GameObject;
			largeAshTile = Resources.Load("LargeAsh") as GameObject;
			smallAshTile = Resources.Load("SmallAsh") as GameObject;
			waterTile = Resources.Load("WaterSplat") as GameObject;
			chemTile = Resources.Load("ChemSplat") as GameObject;
			smallXenoBloodTile = Resources.Load("SmallXenoBloodSplat") as GameObject;
			medXenoBloodTile = Resources.Load("MedXenoBloodSplat") as GameObject;
			largeXenoBloodTile = Resources.Load("LargeXenoBloodSplat") as GameObject;
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
	public static void ChemSplat(Vector3Int worldPos)
	{
		EnsureInit();
		Spawn.ServerPrefab(chemTile, worldPos, MatrixManager.AtPoint(worldPos, true).Objects, Quaternion.identity);
	}
}