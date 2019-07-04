using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GUI_P_Input : PageElement
{
	public InputField TInputField;

	public override bool IsThisType(Type TType)
	{
		return (true);
	}

	public override void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		TInputField.text  = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
	}
}
