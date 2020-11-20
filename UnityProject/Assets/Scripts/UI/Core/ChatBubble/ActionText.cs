using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ActionText : MonoBehaviour
{
	public Vector3 DefaultPosition;

	public RectTransform rectTransform;
	public TMP_Text Text;
	public TMP_Text BackText;
	public float Distancey = 500;
	public float Distancex = 500;
	public float Timey = 5;
	public float Timex = 5;


	public float Decay = 0.95f;

	public bool DoFade = false;


	public GameObject MultiplierGameObject;
	public TMP_Text Multiplier;
	public TMP_Text backMultiplier;

	public Image image;

	public int CountInstance = 1;
	public void SetUp(string InString)
	{
		CountInstance = 1;
		MultiplierGameObject.SetActive(false);
		LeanTween.cancel(this.gameObject);
		this.rectTransform.localPosition = DefaultPosition;
		Text.text = InString;
		BackText.text = InString;
		LeanTween.moveY(rectTransform, Distancey, Timey).setEaseInOutQuad();
		LeanTween.moveX(rectTransform, Distancex, Timex).setEaseInOutQuad();
		DoFade = true;
		this.gameObject.SetActive(true);
	}

	public void AddCopy()
	{
		MultiplierGameObject.SetActive(true);
		CountInstance++;
		var intxt = "X"+CountInstance;
		Multiplier.text = intxt;
		backMultiplier.text = intxt;
		var flot = Text.alpha;
		flot += 0.3f;
		if (flot > 1)
		{
			Text.alpha = 1;
		}

		Text.alpha = flot;


	}

	public void Update()
	{
		if (DoFade)
		{
			float CurrentAlpha = Text.alpha * Decay;
			Text.alpha = CurrentAlpha;
			BackText.alpha = CurrentAlpha;

			var colo = image.color;
			colo.a = CurrentAlpha + 0.2f;
			image.color = colo;
			Multiplier.alpha = CurrentAlpha + 0.2f;
			backMultiplier.alpha = CurrentAlpha + 0.2f;

			if (CurrentAlpha < 0.01)
			{
				LeanTween.cancel(this.gameObject);
				DoFade = false;
				Reset();
			}
		}
	}

	[NaughtyAttributes.Button()]
	public void Reset()
	{
		Text.alpha = 1;
		BackText.alpha = 1;
		DoFade = false;
		this.gameObject.SetActive(false);
		this.rectTransform.SetPositionAndRotation(DefaultPosition, this.rectTransform.rotation);

		var colo = image.color;
		colo.a = 1;
		image.color = colo;
		Multiplier.alpha = 1;
		backMultiplier.alpha = 1;

	}
}