﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OxygenAlert : MonoBehaviour {

	private const float SpriteCycleTime = 1f; // cycle every 1 second
	public Sprite[] statusImages; //images to cycle between when active
	private int nextImageIndex = 1;

	public Image img;
	private float timeWait;

	void Start()
	{
		img = GetComponent<Image>();
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
		img.sprite = statusImages.Wrap( nextImageIndex++ );
	}

	void ResetImg() {
		img.sprite = statusImages[0];
		nextImageIndex = 1;
		timeWait = 0f;
	}
}
