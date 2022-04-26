using System;
using UnityEngine;
using UnityEngine.UI;

namespace Learning
{
	public class ProtipListUI : MonoBehaviour
	{
		[SerializeField] private GameObject entryToSpawn;
		[SerializeField] private Transform entryList;
		[SerializeField] private Dropdown expierenceControlDropdown;


		private void Awake()
		{
			RefreshSOList();
		}

		public void CloseUI()
		{
			gameObject.SetActive(false);
		}

		public void OnChangeDropDownValue()
		{

		}

		private void RefreshSOList()
		{
			foreach (var tipSO in ProtipManager.Instance.RecordedProtips)
			{
				var newEntry = Instantiate(entryToSpawn, entryList);
				newEntry.GetComponent<ProtipListEntry>().Setup(tipSO);
			}
		}
	}
}