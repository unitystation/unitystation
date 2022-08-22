using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUIListTemplate : MonoBehaviour
{
	public GameObject topDetails = null;
	public GameObject expandedDetails = null;

	public TMP_Text playerNumber = null;
	public TMP_Text playerName = null;
	public TMP_Text playerPing;

	public VerticalLayoutGroup layoutGroup = null;

	private RectTransform rtPersonal;
	private RectTransform rtTop;
	private RectTransform rtExpanded;

	private void Awake()
	{
		rtPersonal = GetComponent<RectTransform>();
		rtTop = topDetails.GetComponent<RectTransform>();
		rtExpanded = expandedDetails.GetComponent<RectTransform>();
	}

	public void OnClick()
	{
		if (expandedDetails.activeSelf)
		{
			expandedDetails.SetActive(false);
			rtPersonal.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rtTop.rect.height);
			LayoutSwitch();
		}
		else
		{
			expandedDetails.SetActive(true);
			rtPersonal.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rtTop.rect.height + rtExpanded.rect.height);
			LayoutSwitch();
		}
	}

	private void LayoutSwitch()
	{
		layoutGroup.enabled = false;
		layoutGroup.enabled = true;
	}
}
