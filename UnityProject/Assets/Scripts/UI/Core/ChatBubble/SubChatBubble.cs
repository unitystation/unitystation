using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubChatBubble : MonoBehaviour
{

	public ChatBubble ChatBubble;

	public Image TrailImage;


	/// <summary>
	/// Minimum time a bubble's text will be visible on-screen.
	/// </summary>
	private const float displayTimeMin = 1.8f;

	/// <summary>
	/// Multiplies the elapse of display time. A value of 1.5 would make time elapse 1.5 times as fast. 1 is normal speed.
	/// </summary>
	private float displayTimeMultiplier = 1;

	/// <summary>
	/// The current size of the chat bubble determined by vocalization. Will be scaled by the zoomMultiplier.
	/// </summary>
	private float bubbleSize = 2;

	[SerializeField]
	[Tooltip(
		"The size multiplier of the chat bubble when the player has typed in all caps or ends the sentence with !!.")]
	[Range(1, 100)]
	private float bubbleSizeCaps = 1.5f;

	[SerializeField] private TextMeshProUGUI bubbleText = null;

	[SerializeField] [Tooltip("The default font used when speaking.")]
	private TMP_FontAsset fontDefault = null;

	[SerializeField] [Tooltip("The font used when the player is an abominable clown.")]
	private TMP_FontAsset fontClown = null;

	[SerializeField] [Tooltip("The size multipler of the chat bubble when starts the sentence with #.")] [Range(1, 100)]
	private float bubbleSizeWhisper = 0.5f;

	/// <summary>
	/// Multiplies the elapse of display time per character left in the msgQueue (after the first element).
	/// </summary>
	private static float displayTimeMultiplierPerSecond = 0.09f;

	public void DoShowDialogue(CancellationToken cancelToken, ChatBubble.BubbleMsg msg)
	{
		StartCoroutine(ShowDialogue(cancelToken, msg));
	}

	IEnumerator ShowDialogue(CancellationToken cancelToken, ChatBubble.BubbleMsg msg)
	{
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

		bool showingDialogue = true;
		while (showingDialogue)
		{
			var ASiblingIndex = transform.GetSiblingIndex();

			if (ChatBubble.ActiveBubbles.Count == ASiblingIndex + 1)
			{
				TrailImage.enabled = true;
			}
			else
			{
				TrailImage.enabled = false;
			}

			if (cancelToken.IsCancellationRequested)
			{
				yield break;
			}

			yield return null;

			msg.elapsedTime += Time.deltaTime;

			if (msg.instantText == false)
			{
				ShowCharacter(msg);
			}

			if (msg.elapsedTime * displayTimeMultiplier >= msg.maxTime && msg.elapsedTime >= displayTimeMin)
			{
				var SiblingIndex = transform.GetSiblingIndex();
				if (0  == SiblingIndex)
				{
					showingDialogue = false;
				}
			}
		}

		ChatBubble.ActiveBubbles.Remove(this);
		ChatBubble.PooledBubbles.Add(this);
		gameObject.SetActive(false);
		gameObject.transform.SetParent(ChatBubble.Pool);
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
		else if ((modifiers & ChatModifier.Sing) == ChatModifier.Sing)
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
	}

	/// <summary>
	/// Updates the scale of the chat bubble
	/// </summary>
	private void UpdateChatBubbleSize()
	{
		transform.localScale = new Vector3(bubbleSize, bubbleSize, 1);
	}

	private void CheckShowTime(ChatBubble.BubbleMsg msg)
	{
		var timeLeft = msg.maxTime - (msg.msg.Length * msg.characterPopInSpeed);
		var additionalTime = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleAdditionalTime);

		if (timeLeft >= additionalTime) return;

		//Set max time to the needed amount
		msg.maxTime = additionalTime - timeLeft;
	}

	/// <summary>
	/// Pops in characters from the message over time
	/// </summary>
	private void ShowCharacter(ChatBubble.BubbleMsg msg)
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
}