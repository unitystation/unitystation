using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class GUI_SecurityRecordsEntriesPage : NetPage
{
	[SerializeField]
	private EmptyItemList recordsList = null;
	private GUI_SecurityRecords securityRecordsTab;
	private List<SecurityRecord> currentRecords = new List<SecurityRecord>();

	public void OnOpen(GUI_SecurityRecords recordsTab)
	{
		securityRecordsTab = recordsTab;
		ResetList();
		UpdateTab();
	}

	private void ResetList()
	{
		List<SecurityRecord> records = SecurityRecordsManager.Instance.SecurityRecords;
		
		currentRecords.Clear();
		for (int i = 0; i < records.Count; i++)
		{
			currentRecords.Add(records[i]);
		}
		Debug.Log($"records {records.Count}, currRecords {currentRecords.Count}");
	}

/// <summary>
/// Searches for records containing specific text.
/// WARNING - case sensitive.
/// </summary>
/// <param name="searchText">Text to search</param>
	public void Search(string searchText)
	{
		List<SecurityRecord> newList = new List<SecurityRecord>();
		ResetList();

		Debug.Log($"Searching for -{searchText}-");
		if (searchText.Length > 0 && searchText != " " && !searchText.IsNullOrEmpty())
		{
			foreach (var record in currentRecords)
			{
				if (record.EntryName.Contains(searchText) || record.Age.Contains(searchText) &&
					record.ID.Contains(searchText) || record.Rank.Contains(searchText) ||
					record.Sex.Contains(searchText) || record.Species.Contains(searchText) ||
					record.Fingerprints.Contains(searchText))
					newList.Add(record);
			}
			currentRecords.Clear();
			currentRecords.AddRange(newList);
		}
		Debug.Log($"records after search {currentRecords.Count}");
		UpdateTab();
	}

	public void UpdateTab()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;

		recordsList.Clear();
		recordsList.AddItems(currentRecords.Count);
		for (int i = 0; i < currentRecords.Count; i++)
		{
			GUI_SecurityRecordsItem item = recordsList.Entries[i] as GUI_SecurityRecordsItem;
			item.ReInit(currentRecords[i], securityRecordsTab);
			item.gameObject.SetActive(true);
		}
	}

	public void NewRecord()
	{
		List<SecurityRecord> records = SecurityRecordsManager.Instance.SecurityRecords;
		SecurityRecord record = new SecurityRecord();
		records.Add(record);
		ResetList();
		UpdateTab();
	}
}
