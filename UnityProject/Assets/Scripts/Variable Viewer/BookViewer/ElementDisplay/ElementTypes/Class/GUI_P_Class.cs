using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GUI_P_Class : PageElement
{
	public Button TButton;
	public Text TText;
	public ulong ID;


	public override bool IsThisType(Type TType)
	{
		if (!TType.IsValueType && !(TType == typeof(string)))
		{
			return (true);
		}
		//Need testing
		else if (TType.IsValueType && !TType.IsPrimitive && !(TType == typeof(string)) ){
			Logger.Log(TType.ToString());
			return (true);
		}
		else {
			return (false);
		}
	}

	public override void SetUpValues(Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		TText.text = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
		if (Page != null)
		{
			ID = Page.ID;
		}
		else { 
			//need to set up subsystem for Sentence values
		}
	}

	public void RequestOpenBookOnPage() { 
		OpenPageValueNetMessage.Send(ID);
	}
}
