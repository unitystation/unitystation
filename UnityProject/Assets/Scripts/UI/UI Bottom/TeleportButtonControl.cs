using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TeleportButtonControl : MonoBehaviour
{
	[SerializeField]
	private GameObject buttonTemplate = null;//Sets what button to use in editor
	private GhostTeleport ghostTeleport;
	private TeleportButtonSearchBar SearchBar;
	public List<GameObject> teleportButtons = new List<GameObject>();
	[SerializeField] private TextMeshProUGUI titleText = null;

	//Generates the amount of buttons as the number of entries in the MobList dictionary
	//Buttons for Teleporting To Mobs Tab

	private void Start()
	{
		SearchBar = GetComponentInChildren<TeleportButtonSearchBar>();
	}

	public void GenButtons()
	{
		if (SearchBar != null)
		{
			SearchBar.Resettext();
		}

		titleText.text = "<b>Teleport To Mob</b>";


		foreach (GameObject x in teleportButtons)//resets buttons everytime it opens
		{
			Destroy(x);
		}

		ghostTeleport = GetComponentInParent<GhostTeleport>();
		ghostTeleport.FindData();//Calls the script in GhostTeleport.cs to find the data.
		for (int i = 0; i < ghostTeleport.Count; i++)
		{
			GameObject button = Instantiate(buttonTemplate) as GameObject;//creates new button
			button.SetActive(true);
			var c = button.GetComponent<TeleportButton>();
			c.SetTeleportButtonText(ghostTeleport.MobList[i].Item1 + "\n" + ghostTeleport.MobList[i].Item2 + "\n" + ghostTeleport.MobList[i].Item3);
			c.index = i;//Gives button a number, used to tell which data index is used for teleport
			c.MobTeleport = true;
			teleportButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}

	//Buttons for Teleport Tab
	public void PlacesGenButtons()
	{
		if (SearchBar != null)
		{
			SearchBar.Resettext();
		}

		titleText.text = "<b>Teleport To Location</b>";

		foreach (GameObject x in teleportButtons)//resets buttons everytime it opens
		{
			Destroy(x);
		}
		ghostTeleport = GetComponentInParent<GhostTeleport>();
		ghostTeleport.PlacesFindData();//Calls the script in GhostTeleport.cs to find the place data.
		for (int i = 0; i < ghostTeleport.PlacesCount; i++)
		{
			GameObject button = Instantiate(buttonTemplate) as GameObject;//creates new button
			button.SetActive(true);
			var c = button.GetComponent<TeleportButton>();
			c.SetTeleportButtonText(ghostTeleport.PlacesDict[i].Item1 + "\n" + ghostTeleport.PlacesDict[i].Item2);
			c.index = i;//Gives button a number, used to tell which data index is used for teleport
			teleportButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}
}
