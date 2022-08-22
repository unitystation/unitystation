using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GUI_Info : MonoBehaviour
	{
		public Image Colour;
		public Color banColor;
		public Color infoColor;
		public Text infoText;
		public Text title;

		public void BtnOk()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			gameObject.SetActive(false);

		}

		public void EndEditOnEnter()
		{
			if (KeyboardInputManager.IsEnterPressed())
			{
				BtnOk();
			}
		}

		//	public void Show(string info, string titleText = "")
		//	{
		//		Show(info, infoColor, titleText);
		//	}

		public void Show(string info, bool bwoink, string titleText = "")
		{
			infoText.text = info;
			Colour.color = bwoink ? banColor : infoColor;
			title.text = string.IsNullOrEmpty(titleText) ? "Info" : titleText;
			UIManager.InfoWindow.gameObject.SetActive(true);
			//_ = SoundManager.Play("Bwoink", 1, 1); save Bwoink for admin PM Dont want to spoil it when you get it
		}
	}
}
