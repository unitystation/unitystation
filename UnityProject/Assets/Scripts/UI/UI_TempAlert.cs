using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_TempAlert : MonoBehaviour 
{


    public Sprite[] statusImages; //images to cycle between when active
	private int activeImageIndex = 0;

	private Image img;
	private Sprite sprite;
	private int indexLower;

	void Start ()
	{
		img = GetComponent<Image>();
		sprite = img.sprite;
		InvokeRepeating("CycleImg", 1f, 1f); //Cycle images every 1 second
	}
	
	void CycleImg()
	{	
		if (gameObject.activeInHierarchy)
		{
			if (activeImageIndex >= 2)
			{
				activeImageIndex = indexLower;
			}
			sprite = statusImages[activeImageIndex];
			activeImageIndex++;
			img.sprite = sprite;
		}
		
	}
}





