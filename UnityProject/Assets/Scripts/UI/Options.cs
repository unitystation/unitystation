using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Options : MonoBehaviour
	{
		public Image[] panelImages;
		public Text tipText;
		public Dropdown appearenceDropDown;
		[Header("Corresponds to Appearence Dropdown positions")]
		public UI_Theme[] themes;


		private void Start()
		{
			if(PlayerPrefs.HasKey("UI_Appearence")){
				appearenceDropDown.value = PlayerPrefs.GetInt("UI_Appearence");
				OnAppearenceChange(0);
			}
		}

		//Change the panel theme
		public void OnAppearenceChange(int val){
			PlayerPrefs.SetInt("UI_Appearence", val);
			PlayerPrefs.Save();
			for (int i = 0; i < panelImages.Length; i++){
				panelImages[i].color = themes[appearenceDropDown.value].newPanelColor;
			}

			tipText.color = themes[appearenceDropDown.value].newTipTextColor;
		}
	}

	[System.Serializable]
	public class UI_Theme{
		public Color newPanelColor;
		public Color newTipTextColor;
	}
}


