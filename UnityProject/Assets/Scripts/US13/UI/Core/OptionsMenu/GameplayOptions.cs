using TMPro;
using UnityEngine;
using US13.Learning;
using US13.Managers;

namespace US13.UI.Core.OptionsMenu
{
	public class GameplayOptions : MonoBehaviour
	{
		[SerializeField] private TMP_Dropdown playerExperienceChoices;

		private void OnEnable()
		{
			if (ProtipManager.Instance == null)
			{
				playerExperienceChoices.value = UnityEngine.PlayerPrefs.GetInt(ProtipManager.EXPERIENCE_PREF_KEY, 0);
				return;
			}

			playerExperienceChoices.value = (int)ProtipManager.Instance.PlayerExperienceLevel;
		}

		public void OnPlayerExpChoiceIndexChange()
		{
			ProtipManager.SavePreferredExperienceLevel((ProtipManager.ExperienceLevel)playerExperienceChoices.value);
		}

		public void OnPlayerClick3D()
		{
			if (Manager3D.Instance == null) return;

			Manager3D.Instance.PromptConvertTo3D();
		}
	}
}
