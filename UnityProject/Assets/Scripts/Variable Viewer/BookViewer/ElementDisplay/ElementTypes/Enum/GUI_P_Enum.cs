using System;
using System.Collections.Generic;
using TMPro;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class GUI_P_Enum : PageElement
	{
		public override PageElementEnum PageElementType => PageElementEnum.Enum;
		public TMP_Dropdown TDropdown;

		public override bool IsThisType(Type TType)
		{
			return TType.IsEnum;
		}

		public override void SetUpValues(
				Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null,
				VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
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
				RequestChangeVariableNetMessage.Send(PageID, TDropdown.options[intloc].text, UISendToClientToggle.toggle);
			}
		}


		public override void Pool()
		{
			base.Pool();
			TDropdown.onValueChanged.RemoveAllListeners();
		}
	}
}
