using Antagonists;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CharacterCreator
{
	public class AntagEntry : MonoBehaviour
	{
		// Editor values
		[SerializeField]
		private TMP_Text antagName = null;
		[SerializeField]
		private Button toggle = null;
		[SerializeField]
		private TMP_Text toggleText = null;

		[SerializeField]
		private Color yesColor = new Color(0.1921126f, 0.496f, 0.1117746f);
		[SerializeField]
		private Color noColor = new Color(0.563f, 0.1209127f, 0.08690602f);

		[SerializeField]
		private AntagonistPreferences antagPrefsUI = null;

		// Internal values
		private bool antagEnabled = true;
		private Antagonist antag = null;

		/// <summary>
		/// Set up this newAntag entry
		/// </summary>
		public void Setup(Antagonist newAntag)
		{
			antag = newAntag;
			// Maybe this could include a description and a sprite in future
			antagName.text = newAntag.AntagName;
		}

		/// <summary>
		/// Set this newAntag to be enabled or disabled
		/// </summary>
		public void SetToggle(bool isAntagEnabled)
		{
			antagEnabled = isAntagEnabled;
			toggleText.text = isAntagEnabled ? "Yes" : "No";
			toggle.image.color = isAntagEnabled ? yesColor : noColor;
		}

		/// <summary>
		/// Toggle the enabled status on this newAntag
		/// </summary>
		public void ToggleEnabled()
		{
			SetToggle(!antagEnabled);
			antagPrefsUI.OnToggleChange(antag.AntagName, antagEnabled);
		}
	}
}