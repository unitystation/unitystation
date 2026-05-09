using System;
using Logs;
using TMPro;
using UnityEngine;

public class OptionItem : MonoBehaviour
{

	public Option Option;
	public OptionData OptionData;

	public OptionCollection AssociatedCollection;

	public TMP_Text Label;
	public TMP_Text FailedValidation;

	public void SetUp(Option Option, OptionData OptionData, OptionCollection OptionCollection)
	{
		this.Option = Option;
		this.OptionData = OptionData;
		this.AssociatedCollection = OptionCollection;
		Label.text = OptionData.DisplayName;
		if (PlayerPrefs.HasKey(OptionData.PreferenceKey) == false)
		{
			ResetPreference();
		}

		try
		{
			Populate();
		}
		catch (Exception ex)
		{
			Loggy.Error("error for Option " + Option  + "  " + ex.ToString() );
			ResetPreference();
			Populate();
		}
	}

	public virtual void Populate()
	{

	}

	public virtual void ResetPreference()
	{

	}
}
