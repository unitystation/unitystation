using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionItemSlider : OptionItem
{
	public Slider Slider;

	public override void Populate()
	{
		var (minAndMax, IntScale) = ((Vector2, bool))OptionData.UIParameters.Invoke();

		Slider.wholeNumbers = IntScale;
		Slider.minValue = minAndMax.x;
		Slider.maxValue = minAndMax.y;

		Slider.value = PlayerPrefs.GetFloat(OptionData.PreferenceKey);
		_ = OptionData.OnChangeAction.Invoke(Slider.value);
		Slider.onValueChanged.AddListener(ValueChange);
	}

	public override void ResetPreference()
	{
		PlayerPrefs.SetFloat(OptionData.PreferenceKey, (float)OptionData.Default.Invoke());
		PlayerPrefs.Save();
	}

	public void ValueChange(float value)
	{
		var Return =  OptionData.OnChangeAction.Invoke(value);
		FailedValidation.text = Return;
		if (string.IsNullOrEmpty(Return))
		{
			PlayerPrefs.SetFloat(OptionData.PreferenceKey, value);
		}
		this.AssociatedCollection.OnValChange();
		PlayerPrefs.Save();
	}
}
