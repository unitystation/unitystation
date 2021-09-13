using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

/// <summary>
/// Filters chat messages the player has typed and sends them to the server.
/// Shortens messages if the player typed too much, and frequent messages are also filtered..
/// TODO This might become obsolete with the chat system V2.
/// </summary>
public class ChatFilter : MonoBehaviour
{
	[Header("Max characters per minute")]

	/// <summary>
	/// The maximum number of characters the user is allowed to type per minute.
	/// CPM stands for characters per minute. 1 CPM equals 5 WPM (word per minute).
	/// 250 CPM is considered average, 500 CPM is considered very fast.
	/// </summary>
	[SerializeField]
	[Range(0, 700)]
	[Tooltip("The maximum number of characters the user is allowed to type per minute.")]
	private int cpmMax = 500;

	/// <summary>
	/// How many characters (on average) have been entered within the last minute.
	/// </summary>
	private float cpm = 0;

	/// <summary>
	/// When a message goes above maxCpm they will only be sent to the server if the remaining message has a length of at least minCharsToSend.
	/// </summary>
	[SerializeField]
	[Range(0, 10)]
	[Tooltip("When a message goes above maxCpm they will only be sent to the server if the remaining message has a length of at least minCharsToSend.")]
	private int cpmMinCharacters = 5;

	/// <summary>
	/// What will be shown to the speaker in the chat if they go above the maximum CPM.
	/// </summary>
	[SerializeField]
	[Tooltip("What will be shown to the speaker in the chat if they go above the maximum CPM.")]
	private string cpmWarning = "You ran out of breath before you finished.";

	/// <summary>
	/// What will be shown to the speaker in the chat if they go above the maximum CPM in OOC.
	/// </summary>
	[SerializeField]
	[Tooltip("What will be shown to the speaker in the chat if they go above the maximum CPM in OOC.")]
	private string cpmWarningOOC = "Your messages have been too long, please slow down.";

	[Header("Max messages")]

	/// <summary>
	/// The maximum number of messages the user is allowed to send every messageCoolown minutes.
	/// </summary>
	[SerializeField]
	[Range(0, 10)]
	[Tooltip("The maximum number of messages the user is allowed to send every messageCoolown seconds.")]
	private int numMessageMax = 4;

	/// <summary>
	/// How many messages the user has attempted to send in the past messageCoolown seconds.
	/// </summary>
	private float numMessages = 0;

	/// <summary>
	/// Minutes it takes for numMessages to go back to 0 when starting from maxNumMessages.
	/// </summary>
	[SerializeField]
	[Range(0, 1)]
	[Tooltip("Minutes it takes for numMessages to go back to 0 when starting from maxNumMessages.")]
	private float numMessagesDecayMinutes = 1f / 12f;

	/// <summary>
	/// What will be shown to the speaker in the chat if they go above the maximum messages.
	/// </summary>
	[SerializeField]
	[Tooltip("What will be shown to the speaker in the chat if they go above the maximum messages.")]
	private string numMessagesWarning = "You struggled to speak as you catch your breath.";

	/// <summary>
	/// What will be shown to the speaker in the chat if they go above the maximum messages in OOC.
	/// </summary>
	[SerializeField]
	[Tooltip("What will be shown to the speaker in the chat if they go above the maximum messages in OOC.")]
	private string numMessagesWarningOOC = "You are sending too many messages, please wait a few seconds.";

	/// <summary>
	/// Time the player last sent a message.
	/// </summary>
	private DateTime timeLastMessage;

	/// <summary>
	/// Sends a player message to the server.
	/// The message is omitted if too many messages have been sent recently.
	/// The message is shortened if the player has send too many characters recently.
	/// In either case the player will see a warning in their chat log.
	/// </summary>
	/// <param name="message">The player's message.</param>
	/// <param name="selectedChannels">The selected channels, which are simply passed along.</param>
	public void Send(string message, ChatChannel selectedChannels)
	{
		DecayFiltersOverTime(); // Decrease cpm and messages since last having spoken

		// Limit number of messages
		if (numMessages + 1 > numMessageMax || cpm + 1 > cpmMax)
		{
			if (selectedChannels.HasFlag(ChatChannel.OOC) || selectedChannels.HasFlag(ChatChannel.Ghost))
			{
				Chat.AddExamineMsgToClient(numMessagesWarningOOC);
			}
			else
			{
				Chat.AddExamineMsgToClient(numMessagesWarning);
			}
			return;
		}

		// Users message will (at least partiall) be spoken, so count it.
		numMessages++;
		cpm += message.Length;

		// Limit characters per minute
		int numCharsOverLimit = 0;
		if (cpm > cpmMax)
		{
			// Too many characters, calculate how many need to be removed.
			float cpmOver = cpm - cpmMax;
			cpm = cpmMax; // Characters will be removed, so cpm must be lowered again.
			numCharsOverLimit = (int)Math.Floor(cpmOver);

			message = message.Remove(message.Length - numCharsOverLimit) + "...";
		}

		// Don't send message if it got shortened below the limit.
		if (0 < numCharsOverLimit && numCharsOverLimit < cpmMinCharacters) return;

		// Send message, which might have been shortened because of the character limit per minute.
		PostToChatMessage.Send(message, selectedChannels);

		// Notify player that their message got cut short.
		if (numCharsOverLimit > 0)
		{
			if (selectedChannels.HasFlag(ChatChannel.OOC) || selectedChannels.HasFlag(ChatChannel.Ghost))
			{
				Chat.AddExamineMsgToClient(cpmWarningOOC);
			}
			else
			{
				Chat.AddExamineMsgToClient(cpmWarning);
			}
		}
	}

	/// <summary>
	/// Decreases cpm and NumMessages since the last time the function was called.
	/// </summary>
	private void DecayFiltersOverTime()
	{
		if (timeLastMessage == DateTime.MinValue)
		{
			// This is the first message, nothing needs to decay.
			timeLastMessage = DateTime.Now;
			return;
		}

		TimeSpan timeElapsed = DateTime.Now.Subtract(timeLastMessage);
		float minutesSinceLastMessage = (float)timeElapsed.TotalMinutes;
		timeLastMessage = DateTime.Now;

		cpm = Mathf.Max(0, cpm - (cpmMax * minutesSinceLastMessage));

		numMessages = Mathf.Max(0, numMessages - (numMessageMax * minutesSinceLastMessage * (1 / numMessagesDecayMinutes)));
	}
}
