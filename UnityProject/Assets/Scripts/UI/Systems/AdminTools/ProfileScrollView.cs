using System;
using System.Collections.Generic;
using SecureStuff;
using DatabaseAPI;
using UnityEngine;

namespace AdminTools
{
	public class ProfileScrollView : MonoBehaviour
	{

		[SerializeField] private GameObject profilerEntryPrefab = null;
		[SerializeField] private Transform profileListContent = null;

		private List<ProfileEntry> shownEntries = new List<ProfileEntry>();

		public void RefreshProfileList(SafeProfileManager.ProfileEntryDataList newProfileList)
		{
			var entriesIndex = -1;
			for (var i = 0; i < newProfileList.Profiles.Count; i++)
			{
				entriesIndex++;
				var newProfile = newProfileList.Profiles[i];
				ProfileEntry entry;
				if (shownEntries.Count > i)
				{
					entry = shownEntries[i];
				}
				else
				{
					var newEntry = Instantiate(profilerEntryPrefab, profileListContent);
					entry = newEntry.GetComponent<ProfileEntry>();
					shownEntries.Add(entry);
				}
				entry.fileName.text = newProfile.Name;
				entry.fileSize.text = newProfile.Size;
			}
			for (var i = shownEntries.Count-1; entriesIndex < i; i--) //removing extras
			{
				var entry = shownEntries[i];
				shownEntries.Remove(entry);
				Destroy(entry.gameObject);
			}
		}
	}
}