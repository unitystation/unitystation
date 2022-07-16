using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ActionText : MonoBehaviour
{
	public RectTransform rectTransform;
	public TMP_Text Text;
	public TMP_Text BackText;
	public TMP_Text Multiplier;
	public TMP_Text backMultiplier;
	public GameObject MultiplierGameObject;
	public Image image;

	private float Distancey = 1;
	private float Distancex = 1;
	public float Timey = 5;
	public float Timex = 5;

	private float timeToFade;
	public bool DoFade = false;
	public int CountInstance = 1;

	public void SetUp(string InString, GameObject recipient)
	{
		transform.SetParent(recipient.transform);
		var canvas = GetComponent<Canvas>();
		transform.localScale = new Vector3(0.014f, 0.014f, 0);
		//canvas.sortingLayerID = SortingLayer.NameToID("UI");
		canvas.sortingLayerName = "UI";
		MultiplierGameObject.SetActive(false);
		Text.text = InString;
		BackText.text = InString;
		rectTransform.localPosition = Vector3.zero;
		StartMoving();
		DoFade = true;
		gameObject.SetActive(true);
	}

	private void StartMoving()
	{
		LeanTween.moveY(rectTransform, Distancey, Timey).setEaseInOutQuad();
		LeanTween.moveX(rectTransform, Distancex, Timex).setEaseInOutQuad();
	}

	public void AddMultiplier()
	{
		LeanTween.cancel(rectTransform);
		rectTransform.localPosition = Vector3.zero;
		RestoreAlpha();

		MultiplierGameObject.SetActive(true);
		CountInstance++;
		var intxt = "X"+CountInstance;
		Multiplier.text = intxt;
		backMultiplier.text = intxt;

		StartMoving();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void UpdateMe()
	{
		if (DoFade)
		{
			timeToFade += Time.deltaTime;
			if (timeToFade > 2)
			{
				float CurrentAlpha = Text.alpha - 0.03f;
				Text.alpha = CurrentAlpha;
				BackText.alpha = CurrentAlpha;

				var colo = image.color;
				colo.a = CurrentAlpha + 0.2f;
				image.color = colo;
				Multiplier.alpha = CurrentAlpha + 0.2f;
				backMultiplier.alpha = CurrentAlpha + 0.2f;

				if (CurrentAlpha < 0.01)
				{
					Reset();
				}
			}
		}
	}

	private void RestoreAlpha()
	{
		timeToFade = 0;
		Text.alpha = 1;
		BackText.alpha = 1;
		var colo = image.color;
		colo.a = 1;
		image.color = colo;
		Multiplier.alpha = 1;
		backMultiplier.alpha = 1;
	}

	[NaughtyAttributes.Button()]
	public void Reset()
	{
		CountInstance = 1;
		LeanTween.cancel(rectTransform);
		transform.SetParent(ChatBubbleManager.Instance.transform);
		Text.text = "";
		DoFade = false;
		MultiplierGameObject.SetActive(false);
		gameObject.SetActive(false);
		rectTransform.SetPositionAndRotation(Vector3.zero, rectTransform.rotation);
		RestoreAlpha();
	}
}