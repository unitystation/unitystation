using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GUI_P_Bool : PageElement
{
	public Toggle TToggle;

	public override bool IsThisType(Type TType)
	{
		if (TType == typeof(bool))
		{
			return (true);
		}
		else {
			return (false);
		}
	}

	public override void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		TToggle.isOn = bool.Parse(VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey));

	}
}
