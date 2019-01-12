using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleScreenAnimator : MonoBehaviour
{
	private bool isOn = true;
	public bool IsOn
	{
		get { return isOn; }
		set
		{
			if (value)
			{
				if (!isOn)
				{
					isOn = value;
					sIndex = 0;
					StartCoroutine(Animator());
				}
				else
				{
					isOn = value;
				}
			}
		}
	}
	public float timeBetweenFrames = 0.1f;
	public SpriteRenderer spriteRenderer;
	public GameObject screenGlow;
	public Sprite[] onSprites;

	private int sIndex = 0;

	void Start()
	{
		if (isOn)
		{
			StartCoroutine(Animator());
		}
		else
		{
			spriteRenderer.enabled = false;
			if (screenGlow != null)
			{
				screenGlow.SetActive(false);
			}
		}
	}

	IEnumerator Animator()
	{
		if (screenGlow != null)
		{
			screenGlow.SetActive(true);
		}
		spriteRenderer.enabled = true;
		while (isOn)
		{
			spriteRenderer.sprite = onSprites[sIndex];
			sIndex++;
			if (sIndex == onSprites.Length)
			{
				sIndex = 0;
			}
			yield return new WaitForSeconds(timeBetweenFrames);
		}
		yield return YieldHelper.EndOfFrame;
		spriteRenderer.enabled = false;
		if (screenGlow != null)
		{
			screenGlow.SetActive(false);
		}
	}
}