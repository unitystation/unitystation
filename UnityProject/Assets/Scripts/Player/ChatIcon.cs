using System.Collections;
using UnityEngine;

public class ChatIcon : MonoBehaviour
{
	public Sprite exclaimSprite;
	public Sprite questionSprite;
	private SpriteRenderer spriteRend;
	public Sprite talkSprite;

	private Coroutine coWaitToTurnOff;

	// Use this for initialization
	private void Start()
	{
		spriteRend = GetComponent<SpriteRenderer>();
		spriteRend.enabled = false;
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
	/// </summary>
	/// <param name="chatModifier">The player's chat modifier.</param>
	public void TurnOnTalkIcon(ChatModifier chatModifier)
	{
		switch (chatModifier)
		{
			case ChatModifier.Yell:
				goto case ChatModifier.Exclaim;
			case ChatModifier.Exclaim:
				spriteRend.sprite = exclaimSprite;
				break;
			default:
				spriteRend.sprite = talkSprite;
				break;
		}
		spriteRend.enabled = true;
		if (coWaitToTurnOff != null)
		{
			StopCoroutine(coWaitToTurnOff);
			coWaitToTurnOff = null;
		}
		coWaitToTurnOff = StartCoroutine(WaitToTurnOff());
	}

	public void TurnOffTalkIcon()
	{
		if (coWaitToTurnOff != null)
		{
			StopCoroutine(coWaitToTurnOff);
			coWaitToTurnOff = null;
		}
		spriteRend.enabled = false;
	}

	private IEnumerator WaitToTurnOff()
	{
		yield return WaitFor.Seconds(3f);
		spriteRend.enabled = false;
	}
}