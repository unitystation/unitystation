using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowSpawnX => SpecifiedAction == ActionType.SpawnX;

	[AllowNesting] [ShowIf(nameof(ShowSpawnX))] public SpawnX SpawnXData;

	[System.Serializable]
	public class SpawnX
	{
		public GameObject Prefab;
		public Vector3 PositionToSpawn;
		[Range(1, 100)] public int NumberToSpawn = 1;
		[Range(0, 100)] public int StackableAmount = 0;

		public bool InitiateSpawnX(TestRunSO TestRunSO)
		{
			if (NumberToSpawn == 0)
			{
				NumberToSpawn = 1;
			}

			var Object = Spawn.ServerPrefab(Prefab, PositionToSpawn.RoundToInt(), count: NumberToSpawn);
			if (Object.Successful == false)
			{
				TestRunSO.Report.AppendLine("Unable to spawn prefab " + Prefab);
			}
			else
			{
				if (StackableAmount != 0)
				{
					foreach (var gameObject in Object.GameObjects)
					{
						if (gameObject.TryGetComponent<Stackable>(out var Stack))
						{
							Stack.ServerSetAmount(StackableAmount);
						}
					}
				}
			}

			return true;
		}
	}

	public bool InitiateSpawnX(TestRunSO TestRunSO)
	{
		return SpawnXData.InitiateSpawnX(TestRunSO);
	}
}