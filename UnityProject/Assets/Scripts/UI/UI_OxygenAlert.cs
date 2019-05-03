using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OxygenAlert : MonoBehaviour {

	public Sprite[] statusImages; //images to cycle between when active
	private int activeImageIndex = 0;

	private Image img;
	private Sprite sprite;

	void Awake()
	{
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
		while (true)
		{
			
			
			//Restart "animation"
			if (activeImageIndex >= statusImages.Length)
			{
				activeImageIndex = 0;
			}
			sprite = statusImages[activeImageIndex];
			activeImageIndex++;
			img.sprite = sprite;

			yield return new WaitForSeconds(1f);
		}
	}
}
