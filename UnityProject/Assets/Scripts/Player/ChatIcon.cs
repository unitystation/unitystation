using System.Collections;
using UnityEngine;

public class ChatIcon : MonoBehaviour
{
	public Sprite exlaimSprite;
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

	//TODO needs work
	public void TurnOnTalkIcon()
	{
		spriteRend.sprite = talkSprite;
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
		yield return new WaitForSeconds(3f);
		spriteRend.enabled = false;
	}
}