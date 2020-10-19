using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUI_HackingDevice : MonoBehaviour
{
	private HackingDevice device;
	public HackingDevice Device => device;

	public GUI_Hacking parentHackingPanel;

	[SerializeField]
	private Image itemImage = null;

	public void Start()
	{
		parentHackingPanel = GetComponentInParent<GUI_Hacking>();
	}

	public void SetHackingDevice(HackingDevice device)
	{
		this.device = device;
		SetUpDeviceData();
	}

	private void SetUpDeviceData()
	{
		SpriteRenderer spriteRenderer = device.GetComponentInChildren<SpriteRenderer>();
		if (spriteRenderer != null)
		{
			itemImage.sprite = spriteRenderer.sprite;
		}
	}

	public void RemoveDevice()
	{
		parentHackingPanel.RemoveDevice(this);
	}
}
