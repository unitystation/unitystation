using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DatabaseAPI;
using TMPro;

public class GUI_P_Input : PageElement
{
	public InputFieldFocus TInputField;

	public override bool IsThisType(Type TType)
	{
		return (true);
	}

	public override void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		base.SetUpValues(ValueType, Page, Sentence, Iskey);
		TInputField.text  = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
		TInputField.onEndEdit.AddListener(ToggleValueChanged);
	}

	void ToggleValueChanged(string change)
	{
		if (PageID != 0)
		{
			RequestChangeVariableNetMessage.Send(PageID, change,UISendToClientToggle.toggle, ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public override void Pool()
	{
		base.Pool();
		TInputField.onEndEdit.RemoveAllListeners();
	}
}
