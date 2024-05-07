using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

public class GameGizmoText : GameGizmoTracked
{
	public TMP_Text text;


	public void SetUp(GameObject TrackingFrom, Vector3 Position, string Text, Color Colour, float TextSize = 3)
	{
		SetUp(Position, TrackingFrom);
		text.text = Text;
		text.color = Colour;
		text.fontSize = TextSize;
	}

}
