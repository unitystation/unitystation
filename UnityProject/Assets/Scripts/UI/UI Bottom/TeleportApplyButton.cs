using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportApplyButton : MonoBehaviour
{
	private int X;
	private int Y;

	public void OnClick()
	{
		//grabs input field of x
		var x = gameObject.transform.parent.Find("XCoordinate").gameObject.transform.GetComponent<TeleportXCoordinate>().XCoordinate.text;

		//grabs input field of y
		var y = gameObject.transform.parent.Find("YCoordinate").gameObject.transform.GetComponent<TeleportYCoordinate>().YCoordinate.text;

		//Checks thats theres something in it
		if (x != "" && y != "")
		{
			//turns text of numbers into int
			X = int.Parse(x);
			Y = int.Parse(y);

			//makes new vector of coords
			var newVector = new Vector3(X, Y, 0);

			// Gets server to move player
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostPerformTeleport(newVector);
		}
	}
}
