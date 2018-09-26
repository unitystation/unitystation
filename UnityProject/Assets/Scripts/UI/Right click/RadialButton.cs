using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RadialButton : MonoBehaviour{

	public string FunctionType;
	public GameObject Item;
	public Image Circle;
	public Image Icon;
	public Text title;
	public string Hiddentitle;
	public RadialMenu MyMenu;
	public int DefaultPosition;
	public Color DefaultColour;
	public int MenuDepth;

	public void SetColour (Color Color)
	{ 
		Circle.color = Color;
	}
	public Color ReceiveCurrentColour()
	{
		return(Circle.color);
	}


}

