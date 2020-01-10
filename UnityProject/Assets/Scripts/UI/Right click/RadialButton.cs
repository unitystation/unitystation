using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

public class RadialButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image circle;
	[SerializeField] private Image icon;
	[SerializeField] private Text title;
	private RadialMenu menuControl;
	private Color defaultColour;
	[HideInInspector]
	public Action action;
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

		icon.sprite = menuItem.IconSprite;
		if (menuItem.BackgroundSprite != null)
		{
			circle.sprite = menuItem.BackgroundSprite;
		}

		if (!topLevel)
		{
			title.text = menuItem.Label;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		//if (eventData.pointerEnter != gameObject) return;

		if (isTopLevel)
		{
			menuControl.SelectTopLevelButton(this);
		}
		else
		{
			isSelected = true;
			circle.color = defaultColour + (Color.white / 3f);
			menuControl.Selected = this;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		//if (eventData.pointerEnter != gameObject) return;

		if (!isTopLevel)
		{
			isSelected = false;
			circle.color = defaultColour;
			menuControl.Selected = null;
		}
	}

	public void TopLevelSelectToggle(bool isSelected)
	{
		this.isSelected = isSelected;
		if (isSelected)
		{
			circle.color = defaultColour + (Color.white / 3f);
			menuControl.SetButtonAsLastSibling(this);
			foreach (var btn in childButtons)
			{
				btn.gameObject.SetActive(true);
			}
		}
		else
		{
			circle.color = defaultColour;
			foreach (var btn in childButtons)
			{
				btn.gameObject.SetActive(false);
			}
		}
	}
}