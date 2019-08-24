using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OxygenAlert : MonoBehaviour {

	public Sprite[] statusImages; //images to cycle between when active
	private int activeImageIndex = 0;

	public Image img;
	private Sprite sprite;
	private float timeWait;

	void Start()
	{
		img = GetComponent<Image>();
		sprite = img.sprite;
	}

	private void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	private void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	void UpdateMe()
	{
		timeWait += Time.deltaTime;
		// Cycle images every 1 second
		if (timeWait > 1f)
		{
			CycleImg();
			timeWait = 0f;
		}
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
