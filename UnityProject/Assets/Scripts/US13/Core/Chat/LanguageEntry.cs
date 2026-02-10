using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace US13.Core.Chat
{
	public class LanguageEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text languageNameText = null;

		[SerializeField]
		private TMP_Text languageDescriptionText = null;

		[SerializeField]
		private TMP_Text languagePrefixKeyText = null;

		[SerializeField]
		private Image languageImage = null;

		private LanguageScreen languageScreen;

		private ushort languageId;

		public void SetUp(string languageName, string languageDesc, Sprite languageSprite,
			LanguageScreen setLanguageScreen, ushort setLanguageId, string prefixKey)
		{
			languageNameText.text = languageName;
			languagePrefixKeyText.text = "Prefix:\n," + prefixKey;

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