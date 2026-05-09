using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicOptions
{
	public class OptionItemButton : OptionItem
	{
		public Button Button;

		public override void Populate()
		{
			Button.onClick.AddListener(ValueChange);
		}

		public override void ResetPreference()
		{
			//idk There's not really a default opption
		}

		public void ValueChange()
		{
			var Return = OptionData.OnChangeAction.Invoke("");
			FailedValidation.text = Return;
			this.AssociatedCollection.OnValChange();
		}
	}
}
