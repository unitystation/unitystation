using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the ChatIcon and PlayerChatBubble.
/// Automatically checks PlayerPrefs to determine
/// the use of each one.
/// </summary>
public class PlayerChatBubble : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The maximum length of text inside a single text bubble. Longer texts will display multiple bubbles sequentially.")]
	[Range(1,200)]
	private int maxMessageLength = 70;

	[Header("Size of chat bubble")]

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

	[SerializeField]
	[Tooltip("The maximum scale of text bubbles. When zoomed out very far bubbles will clamp to this scale.")]
	private float maxBubbleScale = 4;

	[SerializeField]
	[Tooltip("The minimum scale of text bubbles. When zoomed out very close bubbles will clamp to this scale.")]
	private float minBubbleScale = 0.55f;

	/// <summary>
	/// The current size of the chat bubble, scaling chatBubble RectTransform.
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
		clown // Clown occupation -> Comic Sans. Which actually looks good on low resolutions. Hmm.
	}

	[SerializeField]
	private ChatIcon chatIcon;
	[SerializeField]
	private GameObject chatBubble;
	[SerializeField]
	private TextMeshProUGUI bubbleText;
	[SerializeField]
	private GameObject pointer;
	class BubbleMsg { public float maxTime; public string msg; public float elapsedTime = 0f; }
	private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
	private bool showingDialogue = false;

	/// <summary>
	/// A cache for the cache bubble rect transform. For performance!
	/// </summary>
	private RectTransform chatBubbleRectTransform;

	/// <summary>
	/// The type of the current chat bubble.
	/// </summary>
	private BubbleType bubbleType = BubbleType.normal;

	void Start()
	{
		chatBubble.SetActive(false);
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

	public void DetermineChatVisual(bool toggle, string message, ChatChannel chatChannel)
	{
		if (!UseChatBubble())
		{
			chatIcon.ToggleChatIcon(toggle);
		}
		else
		{
			AddChatBubbleMsg(message, chatChannel);
		}
	}

	/// <summary>
	/// Determines the bubble type appropriate from the given message.
	/// Refer to BubbleType for further information.
	/// </summary>
	/// <param name="msg"></param>
	private BubbleType GetBubbleType(string msg)
	{
		if (msg.Substring(0, 1).Equals("#")){
			return BubbleType.whisper;
		}
		if (msg.EndsWith("!!") || ((msg.ToUpper(CultureInfo.InvariantCulture) == msg) && msg.Any(System.Char.IsLetter)))
		{
			return BubbleType.caps;
		}
		// TODO Clown occupation check & Somic Sans.

		return BubbleType.normal;
	}


	private void AddChatBubbleMsg(string msg, ChatChannel channel)
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
				if (ws == -1 || ws == 0)ws = maxMessageLength + 2;

				var split = msg.Substring(0, ws);
				msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(split.Length), msg = split });

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxMessageLength)
				{
					msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
				}
			}
		}
		else
		{
			msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
		}

		if (!showingDialogue)StartCoroutine(ShowDialogue());
	}

	IEnumerator ShowDialogue()
	{
		showingDialogue = true;
		if (msgQueue.Count == 0)
		{
			yield return WaitFor.EndOfFrame;
			yield break;
		}
		var b = msgQueue.Dequeue();
		SetBubbleParameters(b.msg);

		while (showingDialogue)
		{
			yield return WaitFor.EndOfFrame;
			b.elapsedTime += Time.deltaTime;
			if (b.elapsedTime >= b.maxTime)
			{
				if (msgQueue.Count == 0)
				{
					bubbleText.text = "";
					chatBubble.SetActive(false);
					showingDialogue = false;
				}
				else
				{
					b = msgQueue.Dequeue();
					SetBubbleParameters(b.msg);
				}
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	/// <summary>
	/// Sets the text of the bubble and the size according to its detected type.
	/// </summary>
	/// <param name="msg"> Player's chat message </param>
	private void SetBubbleParameters(string msg)
	{
		// Set up
		bubbleType = GetBubbleType(msg);

		// Determine type
		switch (bubbleType)
		{
			case BubbleType.caps:
				bubbleSize = bubbleSizeCaps;
				break;
			case BubbleType.whisper:
				bubbleSize = bubbleSizeWhisper;
				msg = msg.Substring(1); // Remove the # symbol from bubble
				break;
			case BubbleType.clown:
				// TODO Implement clown-specific bubble values.
				bubbleSize = bubbleSizeNormal;
				break;
			case BubbleType.normal:
			default:
				bubbleSize = bubbleSizeNormal;
				break;
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
		float bubbleScale = System.Math.Max(minBubbleScale, System.Math.Min(maxBubbleScale, bubbleSize / zoomLevel));
		chatBubbleRectTransform.localScale = new Vector3(bubbleScale, bubbleScale, 1);
	}


	/// <summary>
	/// Used to calculate showing length time
	/// </summary>
	private float TimeToShow(int charCount)
	{
		return Mathf.Clamp((float)charCount / 10f, 2.5f, 10f);
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