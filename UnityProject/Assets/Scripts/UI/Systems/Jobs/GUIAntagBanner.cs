using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI.Jobs
{
	public class GUIAntagBanner: MonoBehaviour
	{
		[SerializeField] private Text antagName = default;
		[SerializeField] private Image background = default;
		public void Show(string textContent, Color textColor, Color backgroundColor)
		{
			///Replace job name by tutorial when in tutorial scene
        
			if(!GameManager.Instance.onTuto)
			{
				antagName.text = textContent;
				antagName.color = textColor;
				background.color = backgroundColor;
			}
			else
			{
				antagName.text = "TUTORIAL";
				antagName.color = Color.white;
				background.color = new Color(0,0,0,0);
			}

			UIManager.Instance.spawnBanner.gameObject.SetActive(true);
			}
	}
}
