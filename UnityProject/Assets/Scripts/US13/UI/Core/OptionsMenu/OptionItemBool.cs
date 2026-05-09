using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionItemBool : OptionItem
{
	public Toggle toggle;

	public override void Populate()
	{
		toggle.isOn = bool.Parse(PlayerPrefs.GetString(OptionData.PreferenceKey));
		_ = OptionData.OnChangeAction.Invoke(toggle.isOn);
		toggle.onValueChanged.AddListener(ValueChange);
	}

	public override void ResetPreference()
	{
		PlayerPrefs.SetString(OptionData.PreferenceKey, OptionData.Default.Invoke().ToString());
	}

	public void ValueChange(bool value)
	{
		var Return =  OptionData.OnChangeAction.Invoke(toggle.isOn);
		FailedValidation.text = Return;
		if (string.IsNullOrEmpty(Return))
		{
			PlayerPrefs.SetString(OptionData.PreferenceKey, value.ToString());
		}
		this.AssociatedCollection.OnValChange();
	}
}
