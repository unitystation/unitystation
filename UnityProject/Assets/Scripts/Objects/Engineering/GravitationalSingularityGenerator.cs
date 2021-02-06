using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitationalSingularityGenerator : MonoBehaviour
{
	[SerializeField]
	private GameObject singularityPrefab = null;

	private RegisterTile registerTile;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	[RightClickMethod]
	private void SpawnSingularity()
	{
		Spawn.ServerPrefab(singularityPrefab, registerTile.WorldPositionServer, gameObject.transform.parent);
	}
}
