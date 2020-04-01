using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using UnityEngine.UI;

public class PlayerAlertView : ChatEntryView
{
	private PlayerAlertData playerAlertData;
	[SerializeField] private Button gibButton = null;
	[SerializeField] private Button takenCareOfButton = null;

	public override void SetChatEntryView(ChatEntryData data, ChatScroll chatScroll, int index, float contentViewWidth)
	{
		base.SetChatEntryView(data, chatScroll, index, contentViewWidth);
		playerAlertData = (PlayerAlertData)data;
		gibButton.interactable = !playerAlertData.gibbed;
		takenCareOfButton.interactable = !playerAlertData.takenCareOf;
	}

	public void GibRequest()
	{

	}

	public void TeleportTo()
	{

	}

	public void TakenCareOf()
	{

	}
}


