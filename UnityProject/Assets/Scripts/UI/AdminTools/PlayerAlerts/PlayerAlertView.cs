using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;

public class PlayerAlertView : ChatEntryView
{
	private PlayerAlertData playerAlertData;
	public override void SetChatEntryView(ChatEntryData data, ChatScroll chatScroll, int index, float contentViewWidth)
	{
		base.SetChatEntryView(data, chatScroll, index, contentViewWidth);
		playerAlertData = (PlayerAlertData)data;
	}

	
}


