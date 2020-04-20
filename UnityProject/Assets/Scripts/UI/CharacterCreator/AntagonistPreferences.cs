using System.Collections.Generic;
using System.Linq;
using Antagonists;
using DatabaseAPI;
using UnityEngine;

namespace UI.CharacterCreator
{
	public class AntagonistPreferences : MonoBehaviour
	{
		[SerializeField]
		private GameObject antagEntryTemplate;

		[SerializeField]
		private AntagData antagData;

		private Dictionary<string, AntagEntry> antagEntries = new Dictionary<string, AntagEntry>();
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
				Logger.LogWarning("Antag entries already populated!");
				return;
			}

			var antags = antagData.GetAllAntags();

			foreach (var antag in antags)
			{
				var newEntryGO = Instantiate(antagEntryTemplate.gameObject, antagEntryTemplate.transform.parent);
				var entry = newEntryGO.GetComponent<AntagEntry>();
				entry.Setup(antag);
				antagEntries.Add(antag.AntagName, entry);
				newEntryGO.SetActive(true);
			}
			isPopulated = true;
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
			antagPrefs = PlayerManager.CurrentCharacterSettings.AntagPreferences;
			if (antagPrefs.Keys.Count == 0)
			{
				// Default all antags to true
				SetAllAntags(true);
				SaveAntagPreferences();
			}

			foreach (var prefKvp in antagPrefs)
			{
				string antagName = prefKvp.Key;
				if (antagEntries.ContainsKey(antagName))
				{
					antagEntries[antagName].SetToggle(prefKvp.Value);
				}
				else
				{
					Logger.LogWarningFormat("There is no antag entry for {0}. Were entries populated incorrectly " +
											"or was the antag renamed?", Category.Antags, antagName);
				}
			}
		}

		/// <summary>
		/// Save all antag preferences to the player manager
		/// </summary>
		private void SaveAntagPreferences()
		{
			PlayerManager.CurrentCharacterSettings.AntagPreferences = antagPrefs;
			ServerData.UpdateCharacterProfile(PlayerManager.CurrentCharacterSettings);
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