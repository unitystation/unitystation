using System.Collections.Generic;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDACrewManifest : NetPage, IPageLifecycle
	{
		[SerializeField]
		private GUI_PDA controller = null;

		[SerializeField]
		private EmptyItemList crewManifestTemplate = null;

		private List<SecurityRecord> storedRecords;

		public void OnPageActivated()
		{
			controller.SetBreadcrumb("/bin/manifest.sh");
			GenerateEntries();
		}

		public void OnPageDeactivated()
		{
			ClearEntries();
		}

		/// <summary>
		/// Generates new Entries for the manifest, making sure to update it just incase it changed
		/// </summary>
		private void GenerateEntries()
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
	}
}
