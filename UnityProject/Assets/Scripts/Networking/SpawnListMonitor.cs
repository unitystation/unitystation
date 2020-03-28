using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnListMonitor : MonoBehaviour
{
	[SerializeField] private CustomNetworkManager networkManager = null;

	void Start()
	{
#if UNITY_EDITOR
		GenerateSpawnList();
#endif
	}

	public bool GenerateSpawnList()
	{
		networkManager.SetSpawnableList();
		return true;
	}
}
