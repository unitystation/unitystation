using UnityEngine;
using UnityEngine.UI;

namespace UI.Jobs
{
	public class GUIAntagBanner: MonoBehaviour
	{
		[SerializeField] private Text antagName;
		[SerializeField] private Image background;
		public void Show(string textContent, Color textColor, Color backgroundColor)
		{
			antagName.text = textContent;
			antagName.color = textColor;
			background.color = backgroundColor;
			UIManager.Instance.antagBanner.gameObject.SetActive(true);
		}
	}
}