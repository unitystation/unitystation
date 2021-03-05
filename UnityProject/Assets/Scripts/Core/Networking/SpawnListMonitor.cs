using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class SpawnListMonitor : MonoBehaviour
{
	[SerializeField] private CustomNetworkManager networkManager = null;

	[Button("Manually fill spawnable prefab list")]
	//usually dynamically filled on build
	public bool GenerateSpawnList()
	{
		networkManager.SetSpawnableList();
		return true;
	}
}
