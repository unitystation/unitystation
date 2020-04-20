using System.Collections;
using System.Collections.Generic;
using Construction;
using UnityEngine;

public class ConveyorBuildMenu : MonoBehaviour
{
	private BuildingMaterial materials;
	private BuildList.Entry entry;

	public void OpenConveyorBuildMenu(BuildList.Entry entry, BuildingMaterial materials)
	{
		this.materials = materials;
		this.entry = entry;
		gameObject.SetActive(true);
	}

	public void TryBuildBelt(int direction)
	{
		SoundManager.Play("Click01");
		Debug.Log("Try build: " + (ConveyorBelt.ConveyorDirection) direction);
	}

	public void GoToMainMenu()
	{
		UIManager.BuildMenu.ShowBuildMenu(materials);
	}

	public void CloseWindow()
	{
		gameObject.SetActive(false);
	}
}
