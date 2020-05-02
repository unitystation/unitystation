using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DatabaseAPI;
using TMPro;

public class GUI_P_Eume : PageElement
{
	public TMP_Dropdown TDropdown;

	public override bool IsThisType(Type TType)
	{
		if (TType.IsEnum)
		{
			return (true);
		}
		else {
			return (false);
		}
	}

	public override void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		base.SetUpValues(ValueType, Page, Sentence, Iskey);
		string Variable = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
		TDropdown.ClearOptions();
		TDropdown.captionText.text = Variable;
		var values = Enum.GetValues(ValueType);
		List<string> llist = new List<string>();
		int Count = 0;
		int Selected = 0;
		foreach (var st in values)
		{
			if (st.ToString() == Variable)
			{
				Selected = Count;
			}
			llist.Add(st.ToString());
			Count++;
		}
		TDropdown.AddOptions(llist);
		TDropdown.value = Selected;
		TDropdown.onValueChanged.AddListener(ToggleValueChanged);
	}

	void ToggleValueChanged(int intloc)
	{
		if (PageID != 0)
		{
			RequestChangeVariableNetMessage.Send(PageID, TDropdown.options[intloc].text, UISendToClientToggle.toggle, ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}


	public override void Pool()
	{
		base.Pool();
		TDropdown.onValueChanged.RemoveAllListeners();
	}
}
