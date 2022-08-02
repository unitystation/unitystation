using Systems.Antagonists;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class EvolveMenuEntry : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text alienNameText = null;

		[SerializeField]
		private TMP_Text alienDescriptionText = null;

		[SerializeField]
		private Image alienImage = null;

		private UI_Alien alien;
		private AlienPlayer.AlienTypes alienType;

		public void SetUp(string alienName, string alienDesc, Sprite alienSprite,
			UI_Alien alienUi, AlienPlayer.AlienTypes alienTypes)
		{
			alienNameText.text = alienName;
			alienImage.sprite = alienSprite;
			alienDescriptionText.text = alienDesc;
			alien = alienUi;
			alienType = alienTypes;
		}

		public void OnEvolve()
		{
			alien.OnEvolve(alienType);
		}
	}
}