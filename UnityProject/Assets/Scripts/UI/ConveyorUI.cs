using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorUI : MonoBehaviour
{
	[SerializeField]
	private ConveyorUIController ConveyorUIController;

	public ConveyorUIController GetController()
	{
		return ConveyorUIController;
	}
}
