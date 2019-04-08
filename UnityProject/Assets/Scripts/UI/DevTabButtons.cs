using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Behavior for the various buttons on the Dev tab
/// </summary>
public class DevTabButtons : MonoBehaviour
{
	public GUI_DevSpawner devSpawner;
	public GUI_DevCloner devCloner;
	public GUI_DevDestroyer devDestroyer;

	public void BtnSpawnItem()
	{
		devCloner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devSpawner.gameObject.SetActive(true);
		devSpawner.Open();
	}

	public void BtnCloneItem()
	{
		devSpawner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(true);
		devCloner.Open();
	}

	public void BtnDestroyItem()
	{
		devSpawner.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(true);
	}
}
