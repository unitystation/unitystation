using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmergencyLightAnimator : MonoBehaviour
{
	public Sprite[] sprites;

	public float animateTime = 0.4f;
	public float rotateSpeed = 30f;

	public bool isOn; //Is turned on (being animated/emissing lights)
	bool isRunningCR = false; //is running coroutine

	private SpriteRenderer spriteRenderer;
	private LightSource lightSource;
	public Color lightColor;

	void Awake()
	{
		lightSource = GetComponent<LightSource>();
		lightSource.customColor = lightColor;
	}
	void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	public void Toggle(bool _isOn)
	{
		if (_isOn && !isRunningCR)
		{
			isOn = _isOn;
			StartCoroutine(Animate());
			lightSource.Trigger(_isOn);
		}
		else
		{
			isOn = _isOn;
			lightSource.Trigger(_isOn);
		}
	}

	IEnumerator Animate()
	{
		isRunningCR = true;
		int curSpriteIndex = 0;
		float counter = 0f;
		spriteRenderer.sprite = sprites[curSpriteIndex];
		while (isOn)
		{
			yield return 0;
			lightSource.mLightRendererObject.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime, Space.World);
			counter += Time.deltaTime;
			if (counter > animateTime)
			{
				counter = 0f;
				curSpriteIndex++;

				if (curSpriteIndex == sprites.Length)
				{
					curSpriteIndex = 0; //Start over
				}
				spriteRenderer.sprite = sprites[curSpriteIndex];
			}
		}
		spriteRenderer.sprite = sprites[2];
		isRunningCR = false;
	}
}