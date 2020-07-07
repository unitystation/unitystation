using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class LavaLandAreaSpawnerScript : MonoBehaviour
{
	public AreaSizes Size;

	[HideInInspector]
	public GameObject prefab;

	private void Start()
	{
		LavaLandManager.Instance.SpawnScripts.Add(this, Size);
	}
}