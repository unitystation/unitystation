using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

public class RadialButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
	[SerializeField] private Image circle;
	[SerializeField] private Image icon;
	[SerializeField] private Text title;
	private string buttonName;
	private RadialMenu menuControl;
	private Color defaultColour;
	private Action action;
	private RightClickMenuItem menuItem;
	private bool isSelected;
	private bool isTopLevel;
	public List<RadialButton> childButtons = new List<RadialButton>();

	public void SetButton(Vector2 localPos, RadialMenu menuController, RightClickMenuItem menuItem, bool topLevel)
	{
		this.menuItem = menuItem;
		transform.localPosition = localPos;
		menuControl = menuController;
		isTopLevel = topLevel;
		circle.color = menuItem.BackgroundColor;
		defaultColour = menuItem.BackgroundColor;
		action = menuItem.Action;
		buttonName = menuItem.Label;

		icon.sprite = menuItem.IconSprite;
		if (menuItem.BackgroundSprite != null)
		{
			circle.sprite = menuItem.BackgroundSprite;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isSelected = true;
		circle.color = defaultColour + (Color.white / 3f);
		if (isTopLevel)
		{
			menuControl.SetButtonAsLastSibling(this);
			foreach (var btn in childButtons)
			{
				btn.gameObject.SetActive(true);
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isSelected = false;
		circle.color = defaultColour;

		if (isTopLevel)
		{
			foreach (var btn in childButtons)
			{
				btn.gameObject.SetActive(false);
			}
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		isSelected = false;
		Debug.Log("POINTER UP ON: " + buttonName);
	}
}