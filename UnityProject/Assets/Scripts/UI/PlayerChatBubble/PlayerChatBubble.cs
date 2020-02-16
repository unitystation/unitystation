using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the ChatIcon and PlayerChatBubble.
/// Automatically checks PlayerPrefs to determine the use of each one.
/// Size of the bubble is determined by a curve relative to the zoom level, and the player's vocalization.
/// </summary>
public class PlayerChatBubble : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The maximum length of text inside a single text bubble. Longer texts will display multiple bubbles sequentially.")]
	[Range(1, 200)]
	private int maxMessageLength = 70;

	[Header("Size of chat bubble")]

	[SerializeField]
	[Tooltip("A multiplier of the bubble size relative to the current zoom level. Gets applied after bubbleSizeNormal, etc.")]
	private AnimationCurve zoomMultiplier;

	[SerializeField]
	[Tooltip("The text size when the player speaks like a normal person.")]
	[Range(1, 100)]
	private float bubbleSizeNormal = 8;

	[SerializeField]
	[Tooltip("The size of the chat bubble when the player has typed in all caps or ends the sentence with !!.")]
	[Range(1, 100)]
	private float bubbleSizeCaps = 12;

	[SerializeField]
	[Tooltip("The size of the chat bubble when starts the sentence with #.")]
	[Range(1, 100)]
	private float bubbleSizeWhisper = 6;

	/// <summary>
	/// The current size of the chat bubble determined by vocalization. Will be scaled by the zoomMultiplier.
	/// </summary>
	private float bubbleSize = 8;

	/// <summary>
	/// Different types of chat bubbles, which might be displayed differently.
	/// TODO Chat.Process.cs has to detect these types of text as well. This detection should be unified to unsure consistent detection.
	/// In addition: ChatModifier exists and *could* be reused here.
	/// </summary>
	enum BubbleType
	{
		normal, // Regular text -> Regular bubble
		whisper, // # -> Smaller bubble
		caps, // All caps (with at least 1 letter) OR end sentence with !! -> Bigger bubble
		clown // Currently not implemented
	}

	[SerializeField]
	[Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault;

	[SerializeField]
	[Tooltip("The font used when the player is an abominable clown.")]
	private TMP_FontAsset fontClown;

	[SerializeField]
	private ChatIcon chatIcon;
	[SerializeField]
	private GameObject chatBubble = null;
	[SerializeField]
	private TextMeshProUGUI bubbleText;
	[SerializeField]
	private GameObject pointer;

	private Camera camera;
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
	/// Multiplies the elapse of display time per character left in the msgQueue (after the first element).
	/// </summary>
	private static float displayTimeMultiplierPerSecond = 0.09f;

	/// <summary>
	/// Multiplies the elapse of display time. A value of 1.5 would make time elapse 1.5 times as fast. 1 is normal speed.
	/// </summary>
	private float displayTimeMultiplier;

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

	void Start()
	{
		chatBubble.SetActive(false);
		camera = Camera.main;
		bubbleText.text = "";
		chatBubbleRectTransform = chatBubble.GetComponent<RectTransform>();
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.ToggleChatBubbles, OnToggle);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.ToggleChatBubbles, OnToggle);
	}

	void Update()
	{
		// Update scale of the chat bubble.
		if (showingDialogue)
		{
			UpdateChatBubbleSize();
		}
		// Correct angle if the player & the bubble get rotated (e.g. on shuttles)
		if (transform.eulerAngles != Vector3.zero)
		{
			transform.eulerAngles = Vector3.zero;
		}
	}

	void OnToggle()
	{
		if (PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleKey) == 0)
		{
			if (showingDialogue)
			{
				StopCoroutine(ShowDialogue());
				showingDialogue = false;
				msgQueue.Clear();
				chatBubble.SetActive(false);
			}
		}
	}

	public void DetermineChatVisual(bool toggle, string message, ChatChannel chatChannel, ChatModifier chatModifier)
	{
		if (!UseChatBubble())
		{
			chatIcon.ToggleChatIcon(toggle, chatModifier);
		}
		else
		{
			AddChatBubbleMsg(message, chatChannel, chatModifier);
		}
	}

	private void AddChatBubbleMsg(string msg, ChatChannel channel, ChatModifier chatModifier)
	{
		// Cancel right away if the player cannot speak.
		if ((chatModifier & ChatModifier.Mute) == ChatModifier.Mute)
		{
			return;
		}

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

		if (!showingDialogue) StartCoroutine(ShowDialogue());
	}

	IEnumerator ShowDialogue()
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
			yield return WaitFor.EndOfFrame;
			msg.elapsedTime += Time.deltaTime;
			if (msg.elapsedTime * displayTimeMultiplier >= msg.maxTime && msg.elapsedTime >= displayTimeMin)
			{
				if (msgQueue.Count == 0)
				{
					bubbleText.text = "";
					chatBubble.SetActive(false);
					showingDialogue = false;
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

	/// <summary>
	/// Sets the text, style and size of the bubble to match the message's modifier.
	/// </summary>
	/// <param name="msg"> Player's chat message </param>
	private void SetBubbleParameters(string msg, ChatModifier modifiers)
	{
		// Set default chat bubble values. Are overwritten by modifiers.
		var screenHeightMultiplier = (float)camera.scaledPixelHeight / 720f; //720 is the reference height
		bubbleSize = bubbleSizeNormal * screenHeightMultiplier;
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
			bubbleSize = bubbleSizeWhisper * screenHeightMultiplier;
			bubbleText.fontStyle = FontStyles.Italic;
			// TODO Differentiate emoting from whispering (e.g. dotted line around text)

		}
		else if ((modifiers & ChatModifier.Yell) == ChatModifier.Yell)
		{
			bubbleSize = bubbleSizeCaps * screenHeightMultiplier;
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
	/// Updates the scale of the chat bubble canvas using bubbleScale and the player's zoom level.
	/// </summary>
	private void UpdateChatBubbleSize()
	{
		int zoomLevel = PlayerPrefs.GetInt(PlayerPrefKeys.CamZoomKey);
		float bubbleScale = (bubbleSize * zoomMultiplier.Evaluate(zoomLevel));
		chatBubbleRectTransform.localScale = new Vector3(bubbleScale, bubbleScale, 1);
	}


	/// <summary>
	/// Used to calculate showing length time
	/// </summary>
	private float TimeToShow(int charCount)
	{
		return Mathf.Clamp((float)charCount * displayTimePerCharacter, displayTimeMin, displayTimeMax);
	}

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
	/// Show the ChatBubble or the ChatIcon
	/// </summary>
	private bool UseChatBubble()
	{
		if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleKey))
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleKey, 0);
			PlayerPrefs.Save();
		}

		return PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleKey) == 1;
	}
}