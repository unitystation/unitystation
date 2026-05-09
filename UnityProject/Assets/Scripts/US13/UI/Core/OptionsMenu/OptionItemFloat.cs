using TMPro;
using UnityEngine;

public class OptionItemFloat : OptionItem
{
	public TMP_InputField InputField;

	public override void Populate()
	{
		InputField.text = PlayerPrefs.GetFloat(OptionData.PreferenceKey).ToString();
		_ = OptionData.OnChangeAction.Invoke(float.Parse(InputField.text));
		InputField.onEndEdit.AddListener(ValueChange);
	}

	public override void ResetPreference()
	{
		PlayerPrefs.SetFloat(OptionData.PreferenceKey, (float)OptionData.Default.Invoke());
		PlayerPrefs.Save();
	}

	public void ValueChange(string value)
	{
		var Return =  OptionData.OnChangeAction.Invoke(float.Parse(value));
		FailedValidation.text = Return;
		if (string.IsNullOrEmpty(Return))
		{
			PlayerPrefs.SetFloat(OptionData.PreferenceKey, float.Parse(value));
		}
		this.AssociatedCollection.OnValChange();
		PlayerPrefs.Save();
	}
}
