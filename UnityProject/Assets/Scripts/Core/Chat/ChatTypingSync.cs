using System;
using System.Collections.Generic;
using Logs;
using Messages.Client;
using UnityEngine;
using UI.Chat_UI;

/// <summary>
/// Sends server when client is actively typing to the chat
/// </summary>
public class ChatTypingSync : MonoBehaviour
{
	[Tooltip("Time after which client consider that player doesn't type anymore")]
	public float typingTimeout = 2f;

	private bool isPlayerTyping;
	private float lastTypingTime;

	private void Start()
	{
		if (ChatUI.Instance == null) return;
		ChatUI.Instance.OnChatInputChanged += OnChatInputChanged;
		ChatUI.Instance.OnChatWindowClosed += OnChatWindowClosed;
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (isPlayerTyping)
		{
			// check if player doesn't enter anything new recently
			var nothingTypedTime = Time.time - lastTypingTime;
			if (nothingTypedTime > typingTimeout)
				StopTyping();
		}
	}

	private void OnChatInputChanged(string newMsg, ChatChannel selectedChannels)
	{
		if (string.IsNullOrEmpty(newMsg))
			return;

		var hasBlacklistedChannel = Chat.NonSpeechChannels.HasFlag(selectedChannels);

		if (isPlayerTyping)
		{
			// update last typing time
			lastTypingTime = Time.time;

			// if player was typing before, but changed channel to blacklist channel
			if (hasBlacklistedChannel)
				StopTyping();
		}
		else
		{
			// if player stared typing in allowed channel
			if (!hasBlacklistedChannel)
				StartTyping();
		}
	}

	private void OnChatWindowClosed()
	{
		StopTyping();
	}

	private void StartTyping()
	{
		if (isPlayerTyping)
			return;

		isPlayerTyping = true;
		lastTypingTime = Time.time;

		ClientTypingMessage.Send(TypingState.TYPING);

		Loggy.Log("Client starts typping to chat", Category.Chat);
	}

	private void StopTyping()
	{
		if (!isPlayerTyping)
			return;

		isPlayerTyping = false;

		ClientTypingMessage.Send(TypingState.STOP_TYPING);

		Loggy.Log("Client stopped typping to chat", Category.Chat);
	}
}
