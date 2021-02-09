using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DatabaseAPI;

public class GUI_P_Bool : PageElement
{
	public override PageElementEnum PageElementType => PageElementEnum.Bool;
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
		TToggle.onValueChanged.AddListener(delegate
		{
			ToggleValueChanged(TToggle);
		});
		base.SetUpValues(ValueType, Page, Sentence, Iskey);

	}
	void ToggleValueChanged(Toggle change)
	{
		if (PageID != 0)
		{
			RequestChangeVariableNetMessage.Send(PageID, change.isOn.ToString(),UISendToClientToggle.toggle, ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public override void Pool()
	{
		base.Pool();
		TToggle.onValueChanged.RemoveAllListeners();
	}

}
