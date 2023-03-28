using Learning;
using TMPro;
using UnityEngine;

namespace Unitystation.Options
{
	public class GameplayOptions : MonoBehaviour
	{
		[SerializeField] private TMP_Dropdown playerExperienceChoices;

		private void OnEnable()
		{
			playerExperienceChoices.value = (int)ProtipManager.Instance.PlayerExperienceLevel;
		}

		public void OnPlayerExpChoiceIndexChange()
		{
			ProtipManager.Instance.SetExperienceLevel((ProtipManager.ExperienceLevel)playerExperienceChoices.value);
		}
		
		public void OnPlayerClick3D()
		{
			GameManager.Instance.PromptConvertTo3D();
		}
	}
}
