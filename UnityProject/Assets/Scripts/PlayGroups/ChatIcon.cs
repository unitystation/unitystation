using System.Collections;
using UnityEngine;

public class ChatIcon : MonoBehaviour
{
	public Sprite exlaimSprite;
	public Sprite questionSprite;
	private SpriteRenderer spriteRend;
	public Sprite talkSprite;

	private bool waitToTurnOff;

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
		if (waitToTurnOff)
		{
			StopCoroutine(WaitToTurnOff());
			waitToTurnOff = false;
		}
		StartCoroutine(WaitToTurnOff());
	}

	public void TurnOffTalkIcon()
	{
		if (waitToTurnOff)
		{
			StopCoroutine(WaitToTurnOff());
		}
		spriteRend.enabled = false;
		waitToTurnOff = false;
	}

	private IEnumerator WaitToTurnOff()
	{
		yield return new WaitForSeconds(3f);
		spriteRend.enabled = false;
		waitToTurnOff = false;
	}
}