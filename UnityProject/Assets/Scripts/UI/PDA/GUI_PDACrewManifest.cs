using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDACrewManifest : NetPage
	{
		[SerializeField]
		private GUI_PDA controller;

		[SerializeField]
		private EmptyItemList crewManifestTemplate;

		private List<SecurityRecord> storedRecords;

		/// <summary>
		/// Generates new Entries for the manifest, making sure to update it just incase it changed
		/// </summary>
		public void GenerateEntries()
		{
			storedRecords = SecurityRecordsManager.Instance.SecurityRecords;
			crewManifestTemplate.Clear();
			crewManifestTemplate.AddItems(storedRecords.Count);
			for (int i = 0; i < storedRecords.Count; i++)
			{
				crewManifestTemplate.Entries[i].GetComponent<GUI_PDAManifestTemplate>()
					.ReInit(storedRecords[i].EntryName, storedRecords[i].Occupation.DisplayName);
			}
		}

		private void ClearEntries()
		{
			crewManifestTemplate.Clear();
		}

		public void Back()
		{
			ClearEntries();
			controller.OpenMainMenu();
		}
	}
}
