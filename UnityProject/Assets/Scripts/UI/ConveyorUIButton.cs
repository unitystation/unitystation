using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConveyorUIButton : MonoBehaviour
{
	[SerializeField]
	private ConveyorUIController conveyorUIController;

	[SerializeField]
	private ConveyorDirection conveyorDirection;

	public Sprite Image;

	public Sprite InvertedImage;

	public GameObject Background;

	public void OnClick()
	{
		conveyorUIController.OnUIClick(conveyorDirection, gameObject);
	}

	public void OnValueChange()
	{
		if (gameObject.GetComponent<ToggleButton>().isOn)
		{
			conveyorUIController.SetInverted(true);
		}
		else
		{
			conveyorUIController.SetInverted(false);
		}
	}

	public void SetImage()
	{
		if (Image == null) return;

		gameObject.GetComponent<Image>().sprite = Image;
	}

	public void SetInvertedImage()
	{
		if (InvertedImage == null) return;
		gameObject.GetComponent<Image>().sprite = InvertedImage;
	}
}
