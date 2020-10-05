using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_PressureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 2) ? "Low Pressure" : "High Pressure";

	public Sprite[] statusImages;
	private int activeImageIndex = -1;

	public Image image;

	public void SetPressureSprite(float pressure)
	{
		if (pressure < 50)
		{
			if (pressure > 20)
			{
				SetSprite(1);	//low pressure
			}
			else
			{
				SetSprite(0);	//really low pressure
			}
		}
		else
		{
			if (pressure > 550)
			{
				SetSprite(3);	//really high pressure
			}
			else
			{
				SetSprite(2);	//high pressure
			}
		}
	}

	void SetSprite(int index)
	{
		if (index == activeImageIndex)
		{
			return;
		}
		activeImageIndex = index;
		image.sprite = statusImages[index];
	}
}
