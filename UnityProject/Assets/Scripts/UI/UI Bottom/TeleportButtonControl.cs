using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportButtonControl : MonoBehaviour
{
	[SerializeField]
	private GameObject buttonTemplate;
	private GhostTeleport ghostTeleport;
	private List<GameObject> teleportButtons = new List<GameObject>();

	public void GenButtons()
	{
		foreach (GameObject x in teleportButtons)
		{
			Destroy(x);
		}

		ghostTeleport = GetComponentInParent<GhostTeleport>();
		ghostTeleport.FindData();
		for (int i = 0; i < ghostTeleport.Count; i++)
		{
			GameObject button = Instantiate(buttonTemplate) as GameObject;
			button.SetActive(true);
			button.GetComponent<TeleportButton>().SetTeleportButtonText(ghostTeleport.MobList[i].Data.s1 + "\n" + ghostTeleport.MobList[i].Data.s2 + "\n" + ghostTeleport.MobList[i].Data.s3);
			button.GetComponent<TeleportButton>().index = i;
			teleportButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}
}
