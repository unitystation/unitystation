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
		while(true)
		{	

			if ( (UIManager.PlayerHealthUI.pressureToggle & RespiratorySystem.PressureChecker.tooLow) != 0)
			{
				indexLower = 0;
			}
			else
			{
				indexLower = 2;
			}
			if (activeImageIndex >= indexLower+2 || activeImageIndex < indexLower) //Restart "animation"
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