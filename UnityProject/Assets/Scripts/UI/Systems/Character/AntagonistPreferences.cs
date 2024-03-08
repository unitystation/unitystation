using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Antagonists;
using UI.Character;

namespace UI.CharacterCreator
{
	public class AntagonistPreferences : MonoBehaviour
	{
		[SerializeField]
		private CharacterSettings characterSettings;

		[SerializeField]
		private GameObject antagEntryTemplate = null;

		[SerializeField]
		private AntagData antagData = null;

		private readonly Dictionary<string, AntagEntry> antagEntries = new();
		private bool isPopulated;

		/// <summary>
		/// Stores the player's antagonist preferences
		/// </summary>
		private AntagPrefsDict antagPrefs = null;

		private void OnEnable()
		{
			PopulateAntags();
			LoadAntagPreferences();
		}

		private void OnDisable()
		{
			SaveAntagPreferences();
		}

		/// <summary>
		/// Populates all antag entries from a list of antags
		/// </summary>
		/// <param name="antags"></param>
		private void PopulateAntags()
		{
			if (isPopulated)
			{
				return;
			}

			var antags = antagData.GetAllAntags().Where(a => a.ShowInPreferences);

			foreach (var antag in antags)
			{
				var newEntryGO = Instantiate(antagEntryTemplate.gameObject, antagEntryTemplate.transform.parent);
				var entry = newEntryGO.GetComponent<AntagEntry>();
				entry.Setup(antag);
				antagEntries.Add(antag.AntagName, entry);
				newEntryGO.SetActive(true);
			}
			isPopulated = true;
			antagEntryTemplate.SetActive(false);
		}

		/// <summary>
		/// Sets all antags to be a certain value
		/// </summary>
		/// <param name="isEnabled"></param>
		public void SetAllAntags(bool isEnabled)
		{
			foreach (string key in antagPrefs.Keys.ToList())
			{
				antagPrefs[key] = isEnabled;
			}
			foreach (var entry in antagEntries.Values)
			{
				entry.SetToggle(isEnabled);
			}
		}

		/// <summary>
		/// Loads all antag prefs from the player manager
		/// </summary>
		private void LoadAntagPreferences()
		{
			antagPrefs = characterSettings.EditedCharacter.AntagPreferences;

			foreach (string antagName in antagPrefs.Keys.ToList())
			{
				if (antagEntries.ContainsKey(antagName))
				{
					antagEntries[antagName].SetToggle(antagPrefs[antagName]);
				}
				else
				{
					// Remove all antag prefs which aren't valid anymore.
					antagPrefs.Remove(antagName);
				}
			}
		}

		/// <summary>
		/// Save all antag preferences to the player manager
		/// </summary>
		private void SaveAntagPreferences()
		{
			characterSettings.EditedCharacter.AntagPreferences = antagPrefs;
		}

		/// <summary>
		/// Called when one of the antag toggles changes value
		/// </summary>
		/// <param name="antagName"></param>
		/// <param name="isAntagEnabled"></param>
		public void OnToggleChange(string antagName, bool isAntagEnabled)
		{
			antagPrefs[antagName] = isAntagEnabled;
		}
	}
}
