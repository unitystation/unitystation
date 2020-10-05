using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeleportButton : MonoBehaviour
{
	[SerializeField]
	public Text myText;

	public int index;

	public bool MobTeleport = false;

	private GhostTeleport ghostTeleport;

	public void SetTeleportButtonText(string textString)
	{
		myText.text = textString;
	}

	public void Onclick()
	{
		ghostTeleport = GetComponentInParent<GhostTeleport>();

		if (MobTeleport == true)
		{
			ghostTeleport.DataForTeleport(index);// Gives index to GhostTeleport.cs
		}
		else
		{
			ghostTeleport.PlacesDataForTeleport(index);// Gives index to GhostTeleport.cs
		}
	}
}
