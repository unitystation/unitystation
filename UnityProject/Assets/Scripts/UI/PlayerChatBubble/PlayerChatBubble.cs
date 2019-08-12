using System.Collections;
using System.Collections.Generic;
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
	private ChatIcon chatIcon;
	[SerializeField]
	private GameObject chatBubble;
	[SerializeField]
	private Text bubbleText;
	[SerializeField]
	private GameObject bg;

	[SerializeField]
	[Range(0f,1000f)]
	private float minWidth;

	[SerializeField]
	[Range(0f,1000f)]
	private float maxWidth;

	class BubbleMsg { public float maxTime; public string msg; public float elapsedTime = 0f; }
	private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
	private bool showingDialogue = false;

	void Start()
	{
		chatBubble.SetActive(false);
		bubbleText.text = "";
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
		if (transform.eulerAngles != Vector3.zero)
		{
			transform.eulerAngles = Vector3.zero;
		}
	}

	void OnToggle()
	{
		if (PlayerPrefs.GetInt(StringManager.ChatBubblePref) == 0)
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

	private void AddChatBubbleMsg(string msg, ChatChannel channel)
	{
		int maxcharLimit = 52;

		if (msg.Length > maxcharLimit)
		{
			while (msg.Length > maxcharLimit)
			{
				int ws = -1;
				//Searching for the nearest whitespace
				for (int i = maxcharLimit; i >= 0; i--)
				{
					if (char.IsWhiteSpace(msg[i]))
					{
						ws = i;
						break;
					}
				}
				//Player is spamming with no whitespace. Cut it up
				if (ws == -1 || ws == 0)ws = maxcharLimit + 2;

				var split = msg.Substring(0, ws);
				msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(split.Length), msg = split });

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxcharLimit)
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
		SetBubbleText(b.msg);

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
					SetBubbleText(b.msg);
				}
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	private void SetBubbleText(string msg)
	{
		chatBubble.SetActive(true);
		bubbleText.text = $"\"{msg}\"";
		AdjustBubbleSize();
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
		if (!PlayerPrefs.HasKey(StringManager.ChatBubblePref))
		{
			PlayerPrefs.SetInt(StringManager.ChatBubblePref, 0);
			PlayerPrefs.Save();
		}

		return PlayerPrefs.GetInt(StringManager.ChatBubblePref) == 1;
	}

	private void AdjustBubbleSize()
	{
		var textRect = bubbleText.GetComponent<RectTransform>();
		if ( textRect.rect.width < minWidth )
		{
			textRect.sizeDelta = new Vector2( minWidth, textRect.sizeDelta.y );
		}

		if ( textRect.rect.width > maxWidth )
		{
			textRect.sizeDelta = new Vector2( maxWidth, textRect.sizeDelta.y );
		}
	}
}