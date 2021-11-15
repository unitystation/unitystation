using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowSpawnX => SpecifiedAction == ActionType.SpawnX;

	[AllowNesting] [ShowIf("ShowSpawnX")] public GameObject Prefab;
	[AllowNesting] [ShowIf("ShowSpawnX")] [Range(1, 100)] public int NumberToSpawn = 1;
	[AllowNesting] [ShowIf("ShowSpawnX")] public Vector3 PositionToSpawn;
	[AllowNesting] [ShowIf("ShowSpawnX")] [Range(0, 100)] public int StackableAmount = 0;


	public bool InitiateSpawnX(TestRunSO TestRunSO)
	{
		if (NumberToSpawn == 0)
		{
			NumberToSpawn = 1;
		}

		var Object = Spawn.ServerPrefab(Prefab, PositionToSpawn, count: NumberToSpawn);
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