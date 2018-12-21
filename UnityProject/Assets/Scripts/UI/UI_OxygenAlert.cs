using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OxygenAlert : MonoBehaviour {

	public Sprite[] statusImages; //images to cycle between when active
	private int activeImageIndex = 0;

	private Image img;
	private Sprite sprite;

	void Start ()
	{
		img = GetComponent<Image>();
		sprite = img.sprite;
		InvokeRepeating("CycleImg", 1f, 1f); //Cycle images every 1 second
	}
	
	void CycleImg()
	{
		sprite = statusImages[activeImageIndex];
		activeImageIndex++;

		//Restart "animation"
		if (activeImageIndex >= statusImages.Length)
		{
			activeImageIndex = 0;
		}

		img.sprite = sprite;
	}
}
