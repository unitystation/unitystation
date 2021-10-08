using System;
using System.Collections.Generic;
using TMPro;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class GUI_P_Input : PageElement
	{
		public override PageElementEnum PageElementType => PageElementEnum.InputField;
		public TMP_InputField TInputField;

		public override bool IsThisType(Type TType)
		{
			return true;
		}

		public override void SetUpValues(
				Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null,
				VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
		{
			base.SetUpValues(ValueType, Page, Sentence, Iskey);
			TInputField.text = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
			TInputField.onEndEdit.AddListener(ToggleValueChanged);
		}

		void ToggleValueChanged(string change)
		{
			if (PageID != 0)
			{
				RequestChangeVariableNetMessage.Send(PageID, change, UISendToClientToggle.toggle);
			}
		}

		public override void Pool()
		{
			base.Pool();
			TInputField.onEndEdit.RemoveAllListeners();
		}
	}
}
