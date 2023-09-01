using Antagonists;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomAntag : Antagonist
{
	private string antagName;
	public new string AntagName => antagName;

	private void Init(string newAntagName)
	{
		antagName = newAntagName;
	}

	public static CustomAntag Create()
	{
		var toRet = ScriptableObject.CreateInstance<CustomAntag>();
		toRet.Init("CustomAntag");
		return toRet;
	}
}