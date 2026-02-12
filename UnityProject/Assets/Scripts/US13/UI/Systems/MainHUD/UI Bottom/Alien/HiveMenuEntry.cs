using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Systems.MainHUD.UI_Bottom.Alien
{
	public class HiveMenuEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text alienNameText = null;

		[SerializeField]
		private Image alienImage = null;

		public void SetUp(string alienName, Sprite alienSprite)
		{
			alienNameText.text = alienName;
			alienImage.sprite = alienSprite;
		}
	}
}