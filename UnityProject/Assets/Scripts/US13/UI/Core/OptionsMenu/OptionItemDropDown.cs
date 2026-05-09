using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class OptionItemDropDown : OptionItem
{
	public TMP_Dropdown DropDown;

	public override void Populate()
	{
		var Option = (List<string>) OptionData.UIParameters.Invoke();
		DropDown.options = 	Option.ToOptionData();

		var Preference = PlayerPrefs.GetString(OptionData.PreferenceKey);

		DropDown.value = Option.IndexOf(PlayerPrefs.GetString(OptionData.PreferenceKey));
		_ = OptionData.OnChangeAction.Invoke(Preference);
		DropDown.onValueChanged.AddListener(ValueChange);
	}



	public override void ResetPreference()
	{
		PlayerPrefs.SetString(OptionData.PreferenceKey, (string)OptionData.Default.Invoke());
	}

	public void ValueChange(int value)
	{
		var TextOpption = DropDown.options[value];
		var Return =  OptionData.OnChangeAction.Invoke(TextOpption.text);
		FailedValidation.text = Return;
		if (string.IsNullOrEmpty(Return))
		{
			PlayerPrefs.SetString(OptionData.PreferenceKey, TextOpption.text);
		}
		this.AssociatedCollection.OnValChange();
	}
}