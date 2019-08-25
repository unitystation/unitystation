using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OxygenAlert : MonoBehaviour {

	private const float SpriteCycleTime = 1f; // cycle every 1 second
	public Sprite[] statusImages; //images to cycle between when active
	private int nextImageIndex = 1;

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
		ResetImg();
	}

	void UpdateMe()
	{
		timeWait += Time.deltaTime;
		if (timeWait > SpriteCycleTime)
		{
			CycleImg();
			timeWait -= SpriteCycleTime;
		}
	}

	void CycleImg()
	{
		sprite = statusImages[nextImageIndex];
		nextImageIndex++;

		//Restart "animation"
		if (nextImageIndex >= statusImages.Length)
		{
			nextImageIndex = 0;
		}

		img.sprite = sprite;
	}

	void ResetImg() {
		sprite = statusImages[0];
		nextImageIndex = 1;
		img.sprite = sprite;
		timeWait = 0f;
	}
}
