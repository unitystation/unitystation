using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class LavaLandAreaSpawnerScript : MonoBehaviour
{
	public AreaSizes Size;

	public bool allowSpecialSites;

	private void Start()
	{
		LavaLandManager.Instance.SpawnScripts.Add(this, Size);
	}
}