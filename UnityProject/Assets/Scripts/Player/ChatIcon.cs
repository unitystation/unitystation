using System.Collections;
using UnityEngine;

public class ChatIcon : MonoBehaviour
{
	// indexes for sprite handler
	private const int TypingSprite = 0;
	private const int QuestionSprite = 1;
	private const int ExclaimSprite = 2;
	private const int TalkSprite = 3;

	[SerializeField]
	private SpriteHandler spriteHandler = null;
	[Tooltip("Time after which chat icon disapear")]
	public float IconTimeout = 3f;
	[Tooltip("The max time of non-stop typing. After this point, player might be disconected")]
	public float TypingTimeout = 20f;

	private Coroutine coWaitToTurnOff;

	// Use this for initialization
	private void Start()
	{
		spriteHandler.gameObject.SetActive(false);
	}

	public void ToggleTypingIcon(bool toggle)
	{
		if (toggle)
		{
			spriteHandler.gameObject.SetActive(true);
			spriteHandler.ChangeSprite(TypingSprite);

			// start coroutine to wait for TypingTimeout
			// if we didn't recived any change for so long - other player must be disconected
			if (coWaitToTurnOff != null)
				StopCoroutine(coWaitToTurnOff);
			coWaitToTurnOff = StartCoroutine(WaitToTurnOff(TypingTimeout));
		}
		else
		{
			spriteHandler.gameObject.SetActive(false);
			if (coWaitToTurnOff != null)
				StopCoroutine(coWaitToTurnOff);
		}
	}

	public void ToggleChatIcon(bool toggle, ChatModifier chatModifier)
	{
		if (toggle)
		{
			TurnOnTalkIcon(chatModifier);
		}
		else
		{
			TurnOffTalkIcon();
		}
	}

	/// <summary>
	/// Turns on the talk icon and picks a suitable icon for the chat modifier.
	/// Starts a coroutine to continue display the icon for a while.
	/// </summary>
	/// <param name="chatModifier">The player's chat modifier.</param>
	public void TurnOnTalkIcon(ChatModifier chatModifier)
	{
		switch (chatModifier)
		{
			case ChatModifier.Yell:
			case ChatModifier.Exclaim:
				spriteHandler.ChangeSprite(ExclaimSprite);
				break;
			case ChatModifier.Question:
				spriteHandler.ChangeSprite(QuestionSprite);
				break;
			default:
				spriteHandler.ChangeSprite(TalkSprite);
				break;
		}
		spriteHandler.gameObject.SetActive(true);
		if (coWaitToTurnOff != null)
		{
			StopCoroutine(coWaitToTurnOff);
			coWaitToTurnOff = null;
		}
		coWaitToTurnOff = StartCoroutine(WaitToTurnOff(IconTimeout));
	}

	public void TurnOffTalkIcon()
	{
		if (coWaitToTurnOff != null)
		{
			StopCoroutine(coWaitToTurnOff);
			coWaitToTurnOff = null;
		}
		spriteHandler.gameObject.SetActive(false);
	}

	private IEnumerator WaitToTurnOff(float time)
	{
		yield return WaitFor.Seconds(time);
		spriteHandler.gameObject.SetActive(false);
	}
}