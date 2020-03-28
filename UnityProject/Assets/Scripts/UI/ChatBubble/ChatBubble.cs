﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour
{
	[SerializeField] private Transform target;
	public Transform Target => target;
	private Camera cam;

	[SerializeField]
	[Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault = null;
	[SerializeField]
	[Tooltip("The font used when the player is an abominable clown.")]
	private TMP_FontAsset fontClown = null;
	[SerializeField]
	private TextMeshProUGUI bubbleText = null;
	[SerializeField]
	[Tooltip("The maximum length of text inside a single text bubble. Longer texts will display multiple bubbles sequentially.")]
	[Range(1, 200)]
	private int maxMessageLength = 70;

	[SerializeField] private GameObject chatBubble = null;

	class BubbleMsg
	{
		public float maxTime; public string msg; public float elapsedTime = 0f;
		internal ChatModifier modifier;
	}
	private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
	private bool showingDialogue = false;

	/// <summary>
	/// A cache for the cache bubble rect transform. For performance!
	/// </summary>
	private RectTransform chatBubbleRectTransform;

	/// <summary>
	/// Minimum time a bubble's text will be visible on-screen.
	/// </summary>
	private const float displayTimeMin = 1.8f;

	/// <summary>
	/// Maximum time a bubble's text will be visible on-screen.
	/// </summary>
	private const float displayTimeMax = 15f;

	/// <summary>
	/// Seconds required to display a single character.
	/// Approximately 102 WPM / 510 CPM with a value of 0.12f
	/// </summary>
	private const float displayTimePerCharacter = 0.12f;

	/// <summary>
	/// When calculating the time it will take to display all chat bubbles in the queue,
	/// each bubble is considered to have this many additional characters.
	/// This causes the queue to be displayed more quickly based on its length.
	/// </summary>
	private const float displayTimeCharactersPerBubble = 10f;

	/// <summary>
	/// Multiplies the elapse of display time per character left in the msgQueue (after the first element).
	/// </summary>
	private static float displayTimeMultiplierPerSecond = 0.09f;

	/// <summary>
	/// Multiplies the elapse of display time. A value of 1.5 would make time elapse 1.5 times as fast. 1 is normal speed.
	/// </summary>
	private float displayTimeMultiplier;

	/// <summary>
	/// The current size of the chat bubble determined by vocalization. Will be scaled by the zoomMultiplier.
	/// </summary>
	private float bubbleSize = 2;

	[SerializeField]
	[Tooltip("The size multiplier of the chat bubble when the player has typed in all caps or ends the sentence with !!.")]
	[Range(1, 100)]
	private float bubbleSizeCaps = 1.2f;

	[SerializeField]
	[Tooltip("The size multipler of the chat bubble when starts the sentence with #.")]
	[Range(1, 100)]
	private float bubbleSizeWhisper = 0.5f;

	private CancellationTokenSource cancelSource;

	/// <summary>
	/// Calculates the time required to display the message queue.
	/// The first message is excluded.
	/// Each bubble is also counted how have a bunch of "fake" characters (displayTimeCharactersPerBubble)
	/// to reduce the delay between each bubble.
	/// </summary>
	/// <param name="queue">The current message queue. The first element will be excluded!</param>
	/// <returns>Time required to display the queue (without the first element).</returns>
	private float TimeToShow(Queue<BubbleMsg> queue)
	{
		float total = 0;
		foreach (BubbleMsg msg in queue.Skip(1))
		{
			total += msg.maxTime;
			total += displayTimePerCharacter * displayTimeCharactersPerBubble;
		}
		return total;
	}

	/// <summary>
	/// Used to calculate showing length time
	/// </summary>
	private float TimeToShow(int charCount)
	{
		return Mathf.Clamp((float)charCount * displayTimePerCharacter, displayTimeMin, displayTimeMax);
	}

	private void OnEnable()
	{
		cam = Camera.main;
		UpdateManager.Add(CallbackType.FIXED_UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.FIXED_UPDATE, UpdateMe);
	}

	public void AppendToBubble(string newMessage, ChatModifier chatModifier = ChatModifier.None)
	{
		QueueMessages(newMessage, chatModifier);
	}

	/// <summary>
	/// Set and enable this ChatBubble
	/// </summary>
	public void SetupBubble(Transform _target, string msg, ChatModifier chatModifier = ChatModifier.None)
	{
		if(cam == null) cam = Camera.main;

		Vector3 viewPos = cam.WorldToScreenPoint(_target.position);
		transform.position = viewPos;

		gameObject.SetActive(true);
		target = _target;
		QueueMessages(msg, chatModifier);

		cancelSource = new CancellationTokenSource();
		if (!showingDialogue) StartCoroutine(ShowDialogue(cancelSource.Token));
	}

	private void QueueMessages(string msg, ChatModifier chatModifier = ChatModifier.None)
	{
		if (msg.Length > maxMessageLength)
		{
			while (msg.Length > maxMessageLength)
			{
				int ws = -1;
				//Searching for the nearest whitespace
				for (int i = maxMessageLength; i >= 0; i--)
				{
					if (char.IsWhiteSpace(msg[i]))
					{
						ws = i;
						break;
					}
				}
				//Player is spamming with no whitespace. Cut it up
				if (ws == -1 || ws == 0) ws = maxMessageLength + 2;

				var split = msg.Substring(0, ws);
				msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(split.Length), msg = split, modifier = chatModifier });

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxMessageLength)
				{
					msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier });
				}
			}
		}
		else
		{
			msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier });
		}

		// Progress quickly through the queue if there is a lot of text left.
		displayTimeMultiplier = 1 + TimeToShow(msgQueue) * displayTimeMultiplierPerSecond;
	}

	IEnumerator ShowDialogue(CancellationToken cancelToken)
	{
		showingDialogue = true;
		if (msgQueue.Count == 0)
		{
			yield return WaitFor.EndOfFrame;
			yield break;
		}
		BubbleMsg msg = msgQueue.Dequeue();
		SetBubbleParameters(msg.msg, msg.modifier);

		while (showingDialogue)
		{
			if (cancelToken.IsCancellationRequested)
			{
				yield break;
			}

			yield return WaitFor.EndOfFrame;
			msg.elapsedTime += Time.deltaTime;
			if (msg.elapsedTime * displayTimeMultiplier >= msg.maxTime && msg.elapsedTime >= displayTimeMin)
			{
				if (msgQueue.Count == 0)
				{
					ReturnToPool();
				}
				else
				{
					msg = msgQueue.Dequeue();
					SetBubbleParameters(msg.msg, msg.modifier);
				}
			}
		}

		yield return WaitFor.EndOfFrame;
	}

    void UpdateMe()
    {
	    if (target != null)
	    {
		    Vector3 viewPos = cam.WorldToScreenPoint(target.position);
		    transform.position = viewPos;
	    }
    }

    /// <summary>
    /// Sets the text, style and size of the bubble to match the message's modifier.
    /// </summary>
    /// <param name="msg"> Player's chat message </param>
    private void SetBubbleParameters(string msg, ChatModifier modifiers)
    {
	    bubbleSize = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize);
	    bubbleText.fontStyle = FontStyles.Normal;
	    bubbleText.font = fontDefault;

	    // Determine type
	    if ((modifiers & ChatModifier.Emote) == ChatModifier.Emote)
	    {
		    bubbleText.fontStyle = FontStyles.Italic;
		    // TODO Differentiate emoting from whispering (e.g. rectangular box instead of speech bubble)
	    }
	    else if ((modifiers & ChatModifier.Whisper) == ChatModifier.Whisper)
	    {
		    bubbleSize = bubbleSize * bubbleSizeWhisper;
		    bubbleText.fontStyle = FontStyles.Italic;
		    // TODO Differentiate emoting from whispering (e.g. dotted line around text)

	    }
	    else if((modifiers & ChatModifier.Sing) == ChatModifier.Sing)
	    {
		    bubbleSize = bubbleSize * bubbleSizeCaps;
		    bubbleText.fontStyle = FontStyles.Italic;
	    }
	    else if ((modifiers & ChatModifier.Yell) == ChatModifier.Yell)
	    {
		    bubbleSize = bubbleSize * bubbleSizeCaps;
		    bubbleText.fontStyle = FontStyles.Bold;
	    }

	    if ((modifiers & ChatModifier.Clown) == ChatModifier.Clown)
	    {
		    bubbleText.font = fontClown;
		    bubbleText.UpdateFontAsset();
	    }

	    // Apply values
	    UpdateChatBubbleSize();
	    chatBubble.SetActive(true);
	    bubbleText.text = msg;
    }

    /// <summary>
    /// Updates the scale of the chat bubble
    /// </summary>
    private void UpdateChatBubbleSize()
    {
	    transform.localScale = new Vector3(bubbleSize, bubbleSize, 1);
    }

    public void ReturnToPool()
    {
	    if(cancelSource != null) cancelSource.Cancel();

	    bubbleText.text = "";
	    chatBubble.SetActive(false);
	    gameObject.SetActive(false);
	    showingDialogue = false;
	    target = null;
    }
}
