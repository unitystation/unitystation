using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuickRadialButton : MonoBehaviour
{

	public Image Circle;
	public Image Icon;
	public Text title;
	public string Hiddentitle;
	public QuickSelectRadial MyMenu;
	public int DefaultPosition;
	public Color DefaultColour;
	public int MenuDepth;
	public Action Action;

	public int AcompanyingButtons;

	public void SetColour (Color Color)
	{
		Circle.color = Color;
	}
	public Color ReceiveCurrentColour()
	{
		return(Circle.color);
	}

	public bool ShouldHideLabel
	{
		get
		{
			return MenuDepth == 100 && AcompanyingButtons > 7;
		}
	}

}

