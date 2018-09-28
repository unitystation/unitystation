using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;

public class RadialButton : MonoBehaviour{

	public GameObject Item;
	public Image Circle;
	public Image Icon;
	public Text title;
	public string Hiddentitle;
	public RadialMenu MyMenu;
	public int DefaultPosition;
	public Color DefaultColour;
	public int MenuDepth;
	public MethodInfo Method;
	public MonoBehaviour Mono;

	public void SetColour (Color Color)
	{ 
		Circle.color = Color;
	}
	public Color ReceiveCurrentColour()
	{
		return(Circle.color);
	}


}

