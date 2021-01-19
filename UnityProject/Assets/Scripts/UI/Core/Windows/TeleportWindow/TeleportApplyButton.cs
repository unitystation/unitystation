using System.Collections;
using System.Collections.Generic;
using UI.Core.Windows;
using UnityEngine;

public class TeleportApplyButton : MonoBehaviour
{
	private int X;
	private int Y;

	[SerializeField] private TeleportWindow mainWindow = default;
	[SerializeField] private GameObject XCoordinate = null;
	[SerializeField] private GameObject YCoordinate = null;

	public void OnClick()
	{
		//grabs input field of x
		var x = XCoordinate.GetComponent<TeleportXCoordinate>().XCoordinate.text;

		//grabs input field of y
		var y = YCoordinate.GetComponent<TeleportYCoordinate>().YCoordinate.text;

		//Checks thats theres something in it
		if (x != "" && y != "")
		{
			//turns text of numbers into int
			X = int.Parse(x);
			Y = int.Parse(y);

			//makes new vector of coords
			var newVector = new Vector3(X, Y, 0);

			mainWindow.TeleportToVector(newVector);
		}
	}
}
