using UnityEngine;

/// <summary>
/// Behavior for the various buttons on the Dev tab
/// </summary>
public class DevTabButtons : MonoBehaviour
{
	public GUI_DevSpawner devSpawner;
	public GUI_DevCloner devCloner;
	public GUI_DevDestroyer devDestroyer;
	public GUI_DevSelectVVTile devSelectTile;
	public GUI_VariableViewer vv;

	public void BtnSpawnItem()
	{
		devCloner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devSelectTile.gameObject.SetActive(false);
		devSpawner.gameObject.SetActive(true);
		devSpawner.Open();
	}

	public void BtnCloneItem()
	{
		devSpawner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devSelectTile.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(true);
		devCloner.Open();
	}

	public void BtnDestroyItem()
	{
		devSpawner.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(false);
		devSelectTile.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(true);
	}

	public void BtnOpenVV()
	{
		devSpawner.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devSelectTile.gameObject.SetActive(false);
		vv.Open();
	}

	public void BtnOpenTileVV()
	{
		devSpawner.gameObject.SetActive(false);
		devCloner.gameObject.SetActive(false);
		devDestroyer.gameObject.SetActive(false);
		devSelectTile.gameObject.SetActive(true);
		devSelectTile.Open();
	}
}