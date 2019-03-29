using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior for the various buttons on the Dev tab
/// </summary>
public class DevTabButtons : MonoBehaviour
{
	public GUI_DevSpawner devSpawner;

	public void BtnSpawnItem()
	{
		devSpawner.gameObject.SetActive(true);
		devSpawner.Open();
	}
}
