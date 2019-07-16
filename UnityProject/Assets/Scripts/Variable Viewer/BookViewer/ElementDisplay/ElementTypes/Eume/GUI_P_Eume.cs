using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GUI_P_Eume : PageElement
{
	public Dropdown TDropdown;

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
	}
}
