using System;
using System.Collections;
using UnityEngine.UI;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class GUI_P_Bool : PageElement
	{
		public override PageElementEnum PageElementType => PageElementEnum.Bool;
		public Toggle TToggle;

		public override bool IsThisType(Type TType)
		{
			return TType == typeof(bool);
		}

		public override void SetUpValues(Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
		{
			TToggle.isOn = bool.Parse(VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey));
			TToggle.onValueChanged.AddListener(delegate
			{
				ToggleValueChanged(TToggle);
			});
			base.SetUpValues(ValueType, Page, Sentence, Iskey);

		}

		private void ToggleValueChanged(Toggle change)
		{
			if (PageID != 0)
			{
				RequestChangeVariableNetMessage.Send(PageID, change.isOn.ToString(), UISendToClientToggle.toggle);
			}
		}

		public override void Pool()
		{
			base.Pool();
			TToggle.onValueChanged.RemoveAllListeners();
		}

	}
}
