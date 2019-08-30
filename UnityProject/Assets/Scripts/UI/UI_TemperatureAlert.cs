using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_TemperatureAlert : TooltipMonoBehaviour
{
	public override string Tooltip => (activeImageIndex < 3) ? "Too Cold" : "Too Hot";

	public Sprite[] statusImages;
	private int activeImageIndex = -1;

	public Image image;


	public void SetTemperatureSprite(float temperature)
	{
		if(temperature < 260)
		{
			if(temperature > 210)
			{
				SetSprite(2);	// a bit cold
			}
			else if(temperature > 160)
			{
				SetSprite(1);	// cold
			}
			else
			{
				SetSprite(0);	// really cold
			}
		}
		else
		{
			if(temperature > 460)
			{
				SetSprite(5);	// superhot
			}
			else if(temperature > 410)
			{
				SetSprite(4);	// hot
			}
			else
			{
				SetSprite(3);	// a bit hot
			}
		}
	}

	void SetSprite(int index)
	{
		if(index == activeImageIndex){
			return;
		}
		activeImageIndex = index;
		image.sprite = statusImages[index];
	}
}
