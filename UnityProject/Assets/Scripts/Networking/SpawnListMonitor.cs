using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnListMonitor : MonoBehaviour
{
	[SerializeField] private CustomNetworkManager networkManager = null;

	public bool GenerateSpawnList()
	{
		networkManager.SetSpawnableList();
		return true;
	}
}
