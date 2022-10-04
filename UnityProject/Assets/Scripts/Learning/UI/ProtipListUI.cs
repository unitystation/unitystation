using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Learning
{
	public class ProtipListUI : MonoBehaviour
	{
		[SerializeField] private GameObject entryToSpawn;
		[SerializeField] private Transform entryList;
		[SerializeField] private Transform mainPage;
		[SerializeField] private Transform settingsPage;
		[SerializeField] private TMP_Dropdown expierenceControlDropdown;

		[SerializeField] private string wikiURL = "https://unitystation.github.io/unitystation-wiki/";


		private void Awake()
		{
			RefreshList();
		}

		public void CloseUI()
		{
			gameObject.SetActive(false);
		}

		public void OnChangeDropDownValue()
		{
			ProtipManager.Instance.SetExperienceLevel((ProtipManager.ExperienceLevel) expierenceControlDropdown.value);
		}

		public void OnPressWikiButton()
		{
			//TODO : ADD THE WIKI IN-GAME PAGE THAT LETS YOU SEARCH ARTICLES TO OPEN LIKE SS13
			Application.OpenURL(wikiURL);
		}

		public void OnPressSettingsPage()
		{
			HideAllPages();
			settingsPage.SetActive(true);
		}

		public void OnPressCatalougePage()
		{
			HideAllPages();
			mainPage.SetActive(true);
		}

		public void OnPressClearAllProtips()
		{
			ProtipManager.Instance.ClearSaveState();
		}

		private void HideAllPages()
		{
			mainPage.SetActive(false);
			settingsPage.SetActive(false);
		}

		private void RefreshList()
		{
			if (entryList.childCount > 0)
			{
				for (int i = 0; i < entryList.childCount - 1; i++)
				{
					Destroy(entryList.GetChild(i));
				}
			}
			foreach (var tipSO in ProtipManager.Instance.RecordedProtips)
			{
				var newEntry = Instantiate(entryToSpawn, entryList);
				newEntry.GetComponent<ProtipListEntry>().Setup(tipSO);
			}
		}
	}
}