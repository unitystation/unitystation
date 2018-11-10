using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyLightAnimator : MonoBehaviour {

	// Experimental animator mostly for show, feel free to configure or rewrite this as needed.

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
		if(isOn && !isRunningCR)
		{
			StartCoroutine(Animate());
		}
	}

	IEnumerator Animate()
	{
		isRunningCR = true;
		int curSpriteIndex = 0;
		spriteRenderer.sprite = sprites[curSpriteIndex];
		while(isOn)
		{
			yield return new WaitForSeconds(animateTime);
			curSpriteIndex++;
			
			if(curSpriteIndex == sprites.Length)
			{
				curSpriteIndex = 0; //Start over
			}
			spriteRenderer.sprite = sprites[curSpriteIndex];
		}
		isRunningCR = false;
	}
}
