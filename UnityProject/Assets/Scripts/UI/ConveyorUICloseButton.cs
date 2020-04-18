using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorUICloseButton : MonoBehaviour
{
	[SerializeField]
	private ConveyorUIController ConveyorUIController;

	public void OnClick()
	{
		ConveyorUIController.CloseUI();
	}
}
