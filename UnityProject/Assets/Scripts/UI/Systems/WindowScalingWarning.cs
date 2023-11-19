using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowScalingWarning : MonoBehaviour
{

	public TMP_Text Textboxs;

	public Image PanelImage;

	public Button Button;

	private void OnEnable()
	{
		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsHeadless) return;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void Close()
	{
		this.gameObject.SetActive(false);
	}

	public void UpdateMe()
	{
		Color oldColor = GUI.color;
		GUI.color = Color.red;

		var cam = Camera.main;
		Vector2Int renderResolution = Vector2Int.zero;
		renderResolution.x = cam.pixelWidth;
		renderResolution.y = cam.pixelHeight;

		if (renderResolution.x % 2 != 0 || renderResolution.y % 2 != 0)
		{
			Button.gameObject.SetActive(true);
			Textboxs.enabled = true;
			string warning = string.Format("Rendering at an odd-numbered resolution ({0} * {1}). Pixel Perfect Camera may not work properly in this situation.", renderResolution.x, renderResolution.y);
			Textboxs.text = warning;
			PanelImage.enabled = true;
		}
		else
		{
			PanelImage.enabled = false;
			Textboxs.enabled = false;
			Button.gameObject.SetActive(false);
		}
	}
}
