using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaLandAreaSpawnerScript : MonoBehaviour
{
	public AreaSizes Size;

	public bool allowSpecialSites;

	private void Start()
	{
		LavaLandManager.Instance.SpawnScripts.Add(this, Size);
	}
}