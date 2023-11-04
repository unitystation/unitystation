using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Logs;
using TMPro;
using UnityEngine;

public class ChatBubble : MonoBehaviour, IDisposable
{
	[SerializeField] private Transform target;
	public Transform Target => target;
	private Camera cam;

	[SerializeField] [Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault = null;

	[SerializeField] [Tooltip("The font used when the player is an abominable clown.")]
	private TMP_FontAsset fontClown = null;

	[SerializeField] private TextMeshProUGUI bubbleText = null;

	[SerializeField]
	[Tooltip(
		"The maximum length of text inside a single text bubble. Longer texts will display multiple bubbles sequentially.")]
	[Range(1, 200)]
	private int maxMessageLength = 70;

	[SerializeField] private GameObject chatBubble = null;

	class BubbleMsg
	{
		public float maxTime;
		public string msg;
		public float elapsedTime = 0f;

		//Character showing delay vars
		internal int characterIndex = 0;

		//Use invis characters so text box doesnt resize
		//keeps characters in the same place as characters appear (easier to read)
		public bool buffered = true;

		//Text appears instantly instead of pop in, from player prefs
		public bool instantText = false;

		//Character pop in speed, from player prefs
		//TODO maybe add override?
		internal float characterPopInSpeed;

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
	[Tooltip(
		"The size multiplier of the chat bubble when the player has typed in all caps or ends the sentence with !!.")]
	[Range(1, 100)]
	private float bubbleSizeCaps = 1.2f;

	[SerializeField] [Tooltip("The size multipler of the chat bubble when starts the sentence with #.")] [Range(1, 100)]
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
		return Mathf.Clamp((float) charCount * displayTimePerCharacter, displayTimeMin, displayTimeMax);
	}

	private void OnEnable()
	{
		cam = Camera.main;
		UpdateManager.Add(CallbackType.POST_CAMERA_UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.POST_CAMERA_UPDATE, UpdateMe);
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
		if (cam == null) cam = Camera.main;

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
				msgQueue.Enqueue(new BubbleMsg
					{maxTime = TimeToShow(split.Length), msg = split, modifier = chatModifier});

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxMessageLength)
				{
					msgQueue.Enqueue(new BubbleMsg
						{maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier});
				}
			}
		}
		else
		{
			msgQueue.Enqueue(new BubbleMsg {maxTime = TimeToShow(msg.Length), msg = msg, modifier = chatModifier});
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

		DoBubble:

		BubbleMsg msg = msgQueue.Dequeue();

		//Sets the chat text to instant from player prefs, 1 == true
		msg.instantText = PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleInstant) == 1;

		bubbleSize = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize);

		if (msg.instantText)
		{
			SetBubbleParameters(msg.msg, msg.modifier);
		}
		else
		{
			//Sets the chat character pop in speed from player prefs
			msg.characterPopInSpeed = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubblePopInSpeed);

			//See if the show time needs updating for speed
			CheckShowTime(msg);
		}

		while (showingDialogue)
		{
			if (cancelToken.IsCancellationRequested)
			{
				yield break;
			}

			yield return WaitFor.EndOfFrame;

			msg.elapsedTime += Time.deltaTime;

			if (msg.instantText == false)
			{
				ShowCharacter(msg);
			}

			if (msg.elapsedTime * displayTimeMultiplier >= msg.maxTime && msg.elapsedTime >= displayTimeMin)
			{
				if (msgQueue.Count == 0)
				{
					ReturnToPool();
				}
				else
				{
					goto DoBubble;
				}
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	/// <summary>
	/// Pops in characters from the message over time
	/// </summary>
	private void ShowCharacter(BubbleMsg msg)
	{
		while (msg.elapsedTime > 0f)
		{
			if (msg.characterIndex > msg.msg.Length - 1) break;

			msg.elapsedTime -= msg.characterPopInSpeed;
			var currentCharacter = msg.msg[msg.characterIndex];

			if (char.IsWhiteSpace(currentCharacter))
			{
				msg.elapsedTime -= msg.characterPopInSpeed * 2f;
			}
			else if (char.IsPunctuation(currentCharacter))
			{
				msg.elapsedTime -= msg.characterPopInSpeed * 3f;
			}

			var text = msg.msg.Substring(0, msg.characterIndex + 1);
			var newText = new StringBuilder();

			//God Save Our Eyes
			if ((msg.modifier & ChatModifier.Clown) == ChatModifier.Clown &&
			    PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleClownColour) == 1)
			{
				for (int i = 0; i < text.Length; i++)
				{
					newText.Append($"<color=#{RandomUtils.CreateRandomBrightColorString()}>{text[i]}</color>");
				}
			}
			else
			{
				newText.Append(text);
			}

			if (msg.buffered && msg.characterIndex < msg.msg.Length - 1)
			{
				//Add the rest of the character but invisible to make bubble correct size
				//and keep the characters in the same place (helps reading)
				newText.Append($"<color=#00000000>{msg.msg.Substring(msg.characterIndex + 1)}</color>");
			}

			SetBubbleParameters(newText.ToString(), msg.modifier);

			msg.characterIndex++;
		}
	}

	private void CheckShowTime(BubbleMsg msg)
	{
		var timeLeft = msg.maxTime - (msg.msg.Length * msg.characterPopInSpeed);
		var additionalTime = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime);

		if (timeLeft >= additionalTime) return;

		//Set max time to the needed amount
		msg.maxTime = additionalTime - timeLeft;
	}

	void UpdateMe()
    {
	    if (target != null)
	    {
		    Vector3 viewPos = manualWorldToScreenPoint(target.position);
		    transform.position = viewPos;
	    }
    }

	Vector3 manualWorldToScreenPoint(Vector3 wp) {
		// calculate view-projection matrix

		var worldToCameraMatrix  = Matrix4x4.Inverse(Matrix4x4.TRS(
			cam.transform.position,
			cam.transform.rotation,
			new Vector3(1, 1, -1)
		) );

		Matrix4x4 mat = cam.projectionMatrix * worldToCameraMatrix;

		// multiply world point by VP matrix
		Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

		if (temp.w == 0f) {
			// point is exactly on camera focus point, screen point is undefined
			// unity handles this by returning 0,0,0
			return Vector3.zero;
		} else {
			// convert x and y from clip space to window coordinates
			temp.x = (temp.x/temp.w + 1f)*.5f * cam.pixelWidth;
			temp.y = (temp.y/temp.w + 1f)*.5f * cam.pixelHeight;
			return new Vector3(temp.x, temp.y, wp.z);
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
	    bubbleText.text = msg;
	    chatBubble.SetActive(true);
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

    public void Dispose()
    {
	    cancelSource?.Dispose();
    }
}
