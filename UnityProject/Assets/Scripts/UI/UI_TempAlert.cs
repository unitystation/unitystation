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
		indexLower = 0;
	}

	void OnEnable()
	{
		StartCoroutine (CycleImg()); // Cycle images every one second

	}
	
	void OnDisable()
	{
		StopCoroutine (CycleImg()); // Ends image cycling

	}
	
	IEnumerator CycleImg()
	{	
		while (true)
		{

			
			if (activeImageIndex >= 2)
			{
				activeImageIndex = indexLower;
			}
			sprite = statusImages[activeImageIndex];
			activeImageIndex++;
			img.sprite = sprite;
			
			yield return new WaitForSeconds(1f);
		}

		
	}
}





