using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportButtonControl : MonoBehaviour
{
	[SerializeField]
	private GameObject buttonTemplate;//Sets what button to use in editor
	private GhostTeleport ghostTeleport;
	private List<GameObject> teleportButtons = new List<GameObject>();

	//Generates the amount of buttons as the number of entries in the MobList dictionary
	public void GenButtons()
	{
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
			button.GetComponent<TeleportButton>().SetTeleportButtonText(ghostTeleport.MobList[i].Data.s1 + "\n" + ghostTeleport.MobList[i].Data.s2 + "\n" + ghostTeleport.MobList[i].Data.s3);
			button.GetComponent<TeleportButton>().index = i;//Gives button a number, used to tell which data index is used for teleport
			teleportButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}
}
