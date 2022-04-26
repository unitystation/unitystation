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
		[SerializeField] private TMP_Dropdown expierenceControlDropdown;


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