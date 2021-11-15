using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public partial class TestAction
{
	public bool ShowSpawnX => SpecifiedAction == ActionType.SpawnX;

	[AllowNesting] [ShowIf("ShowSpawnX")] public GameObject Prefab;
	[AllowNesting] [ShowIf("ShowSpawnX")] [Range(1, 100)] public int NumberToSpawn = 1;
	[AllowNesting] [ShowIf("ShowSpawnX")] public Vector3 PositionToSpawn;
	[AllowNesting] [ShowIf("ShowSpawnX")] [Range(1, 100)] public int StackableAmount = 1;


	public void InitiateSpawnX()
	{
	}
}