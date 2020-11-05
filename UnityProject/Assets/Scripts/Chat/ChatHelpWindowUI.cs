using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Window showing all available channels and their prefixes
/// </summary>
public class ChatHelpWindowUI : WindowDrag
{
	[SerializeField] private Text channelsText;

	private void OnEnable()
	{
		UpdatePossibleChannels();
	}

	private void UpdatePossibleChannels()
	{
		// get available channels
		ChatChannel chatChannels = ChatUI.Instance.GetAvailableChannels();
		// get all chat channels
		ChatChannel[] allChannels = (ChatChannel[])Enum.GetValues(typeof(ChatChannel));

		string newContent = string.Empty;
		// start from 4 to skip 'None', 'Examine', 'Local', 'OOC'
		for (int i = 4; i < allChannels.Length; i++)
		{
			ChatChannel channel = allChannels[i];
			// player have access to this channel
			if (chatChannels.HasFlag(channel))
			{
				newContent += GetChannelEntry(channel);
			}

		}

		channelsText.text = newContent;
	}

	/// <summary>
	/// Get channel entry text.
	/// returned string will be displayed in channelsText, so remember to put '\n' on end
	/// </summary>
	private string GetChannelEntry(ChatChannel channel)
	{
		return $"{channel} = <size=30>{channel.GetDescription()}</size>\n";
	}

	/// <summary>
	/// Disable this window - used in UI button onclick
	/// </summary>
	public void CloseOpenWindow()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}
}
