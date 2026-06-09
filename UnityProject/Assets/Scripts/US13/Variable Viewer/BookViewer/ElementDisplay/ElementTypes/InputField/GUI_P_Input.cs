using System;
using TMPro;
using US13.Messages.Client.VariableViewer;

namespace US13.Variable_Viewer.BookViewer.ElementDisplay.ElementTypes.InputField
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
			RequestChangeVariableNetMessage.Send(PageID, change, UISendToClientToggle.toggle, SentenceID, false);
		}

		public override void Pool()
		{
			base.Pool();
			TInputField.onEndEdit.RemoveAllListeners();
		}
	}
}
