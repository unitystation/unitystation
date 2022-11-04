using System;
using Learning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class TooltipHoverManager : MonoBehaviour
	{
		private Transform activeTooltip;
		[SerializeField] private TMP_Text defaultTooltip;
		[SerializeField] private Text classicTooltip;

		private void Awake()
		{
			if (PlayerPrefs.HasKey(PlayerPrefKeys.EnableClassicHoverTooltip) == false)
				activeTooltip = defaultTooltip.transform;
		}

		public void UpdateActiveTooltip(string tip)
		{
			if (activeTooltip == null) return;

			if (activeTooltip == defaultTooltip.transform && DefaultTooltipChecks())
			{
				defaultTooltip.text = tip;
				return;
			}
			//classic tooltip does not have any extra checks.
			classicTooltip.text = tip;
		}

		public void SetActiveTransform(bool transformNumber)
		{
			activeTooltip = transformNumber ? classicTooltip.transform : defaultTooltip.transform;
		}

		private bool DefaultTooltipChecks()
		{
			// If protip is still not loaded in or player has set their experience level to Robust, hide the tooltip
			// away from them.
			if (ProtipManager.Instance == null ||
			    ProtipManager.Instance.PlayerExperienceLevel == ProtipManager.ExperienceLevel.Robust) return false;
			if (ProtipManager.Instance.PlayerExperienceLevel == ProtipManager.ExperienceLevel.SomewhatExperienced)
			{
				// If the player set their experience level to somewhat experienced, show only if shift is pressed.
				if(KeyboardInputManager.IsShiftPressed() == false) return false;
			}

			return true;
		}
	}
}