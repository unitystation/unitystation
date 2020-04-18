using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConveyorUIController : MonoBehaviour
{
	[SerializeField]
	private List<GameObject> Buttons = new List<GameObject>();

	private GameObject selectedButton;

	private ConveyorBelt selectedConveyor;

	public bool IsInverted;

	public ConveyorDirection selectedConveyorDirection;

	public void OnUIClick(ConveyorDirection direction, GameObject button)
	{
		if (selectedButton == null)
		{
			selectedConveyorDirection = direction;
			button.GetComponent<ConveyorUIButton>().Background.SetActive(true);
		}
		else
		{
			selectedButton.GetComponent<ConveyorUIButton>().Background.SetActive(false);

			selectedButton = button;

			selectedConveyorDirection = direction;

			button.GetComponent<ConveyorUIButton>().Background.SetActive(true);

			selectedConveyor.CmdChangeDirection((ConveyorBelt.ConveyorDirection)selectedConveyorDirection);
		}
	}

	public void SetInverted(bool isInverted)
	{
		IsInverted = isInverted;

		foreach (GameObject button in Buttons)
		{
			if (isInverted)
			{
				button.GetComponent<ConveyorUIButton>().SetInvertedImage();
			}
			else
			{
				button.GetComponent<ConveyorUIButton>().SetImage();
			}
		}
	}

	public void Process(ConveyorBelt conveyor)
	{
		selectedConveyor = conveyor;
	}

	public void OpenUI()
	{
		gameObject.SetActive(true);
	}

	public void CloseUI()
	{
		gameObject.SetActive(false);
	}
}
public enum ConveyorDirection
{
	Up = 0,
	Down = 1,
	Left = 2,
	Right = 3,
	LeftDown = 4,
	UpLeft = 5,
	DownRight = 6,
	RightUp = 7
}
