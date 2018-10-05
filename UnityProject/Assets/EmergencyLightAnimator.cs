using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyLightAnimator : MonoBehaviour {

	public Sprite[] sprites; //First 4 sprites are the "turned on" sprites, last one is off one.
	//public Sprite curSprite;

	public float animateTime = 0.4f;
	int maxSpriteIndex = 3;

	public bool isOn; //Is turned on (being animated)
	bool isRunningCR = false; //is running coroutine


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
		GetComponent<SpriteRenderer>().sprite = sprites[curSpriteIndex];
		while(enabled)
		{
			yield return new WaitForSeconds(animateTime);
			curSpriteIndex++;
			GetComponent<SpriteRenderer>().sprite = sprites[curSpriteIndex];
			if(curSpriteIndex == maxSpriteIndex)
			{
				curSpriteIndex = -1; //Start over
			}
		}

	}
}
