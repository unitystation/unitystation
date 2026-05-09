using TMPro;
using UnityEngine;
using Util;

namespace DynamicOptions
{
	public class OptionItemVector2 : OptionItem
	{
		public TMP_InputField InputFieldx;
		public TMP_InputField InputFieldy;


		public override void Populate()
		{
			var Vector = PlayerPrefs.GetString(OptionData.PreferenceKey).ToVector2();
			InputFieldx.text = Vector.x.ToString();
			InputFieldy.text = Vector.y.ToString();

			_ = OptionData.OnChangeAction.Invoke(Vector);
			InputFieldx.onEndEdit.AddListener(ValueChange);
			InputFieldy.onEndEdit.AddListener(ValueChange);
		}

		public override void ResetPreference()
		{
			PlayerPrefs.SetString(OptionData.PreferenceKey, ((Vector2) OptionData.Default.Invoke()).Serialise());
			PlayerPrefs.Save();
		}



		public void ValueChange(string value)
		{
			var Vector2 = new Vector2(float.Parse(InputFieldx.text), float.Parse(InputFieldy.text));

			var Return = OptionData.OnChangeAction.Invoke(Vector2);
			FailedValidation.text = Return;
			if (string.IsNullOrEmpty(Return))
			{
				PlayerPrefs.SetString(OptionData.PreferenceKey, Vector2.Serialise());
			}

			this.AssociatedCollection.OnValChange();
			PlayerPrefs.Save();
		}
	}
}
