using TMPro;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Chat
{
	public class LanguageEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text languageNameText = null;

		[SerializeField]
		private TMP_Text languageDescriptionText = null;

		[SerializeField]
		private Image languageImage = null;

		private LanguageScreen languageScreen;

		private ushort languageId;

		public void SetUp(string languageName, string languageDesc, Sprite languageSprite,
			LanguageScreen setLanguageScreen, ushort setLanguageId)
		{
			languageNameText.text = languageName;

			languageImage.SetActive(true);

			languageImage.sprite = languageSprite;

			if (languageSprite == null)
			{
				languageImage.SetActive(false);
			}

			languageDescriptionText.text = languageDesc;
			languageId = setLanguageId;
			languageScreen = setLanguageScreen;
		}

		public void OnSelect()
		{
			languageScreen.OnSelect(languageId);
		}
	}
}