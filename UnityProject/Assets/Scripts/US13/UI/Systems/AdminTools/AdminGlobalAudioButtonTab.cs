using UnityEngine;

namespace US13.UI.Systems.AdminTools
{
	public class AdminGlobalAudioButtonTab : MonoBehaviour
	{
		[SerializeField] private GameObject globalSoundWindow = null;

		public void OnClick()
		{
			if (!globalSoundWindow.activeInHierarchy)
			{
				globalSoundWindow.SetActive(true);
			}
			else
			{
				globalSoundWindow.SetActive(false);
			}
		}
	}
}
