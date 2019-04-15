using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_PressureAlert : MonoBehaviour {

    public Sprite[] statusImages; //images to cycle between when active
	private int activeImageIndex = 0;

	private Image img;
	private Sprite sprite;

	private int indexLower;


	void Start ()
	{
		// Cycles the high pressure or low pressure images depending on the value

		img = GetComponent<Image>();
		sprite = img.sprite;
		InvokeRepeating("CycleImg", 1f, 1f); //Cycle images every 1 second
	}
	
	void CycleImg()
	{	
		if (gameObject.activeInHierarchy)
		{
			if ( UIManager.PlayerHealthUI.pressureToggle == 1)
			{
				indexLower = 0;
			}
			else
			{
				indexLower = 2;
			}
			//Restart "animation"
			if (activeImageIndex >= indexLower+2 || activeImageIndex < indexLower)
			{
				activeImageIndex = indexLower;
			}
			sprite = statusImages[activeImageIndex];
			activeImageIndex++;
			img.sprite = sprite;
		}
	}
}