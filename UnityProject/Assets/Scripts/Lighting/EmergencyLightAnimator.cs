using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyLightAnimator : MonoBehaviour
{
	public Sprite[] sprites;

	public float animateTime = 0.4f;

	public bool isOn; //Is turned on (being animated/emissing lights)
	bool isRunningCR = false; //is running coroutine

	private SpriteRenderer spriteRenderer;

	void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	void Update()
	{
		if (isOn && !isRunningCR)
		{
			StartCoroutine(Animate());
		}
	}

	public void Toggle(bool _isOn)
	{
		if (_isOn && !isRunningCR)
		{
			isOn = _isOn;
			StartCoroutine(Animate());
		}
		else
		{
			isOn = _isOn;
		}
	}

	IEnumerator Animate()
	{
		isRunningCR = true;
		int curSpriteIndex = 0;
		spriteRenderer.sprite = sprites[curSpriteIndex];
		while (isOn)
		{
			yield return new WaitForSeconds(animateTime);
			curSpriteIndex++;

			if (curSpriteIndex == sprites.Length)
			{
				curSpriteIndex = 0; //Start over
			}
			spriteRenderer.sprite = sprites[curSpriteIndex];
		}
		spriteRenderer.sprite = sprites[2];
		isRunningCR = false;
	}
}