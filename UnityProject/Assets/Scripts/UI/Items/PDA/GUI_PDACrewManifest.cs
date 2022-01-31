using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems;

namespace UI.Items.PDA
{
	public class GUI_PDACrewManifest : NetPage, IPageLifecycle
	{
		[SerializeField]
		private GUI_PDA controller = null;

		[SerializeField]
		private EmptyItemList crewManifestTemplate = null;

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
		/// Generates new entries for the manifest.
		/// </summary>
		private void GenerateEntries()
		{
			List<CrewManifestEntry> crewManifest = CrewManifestManager.Instance.CrewManifest;
			crewManifestTemplate.AddItems(crewManifest.Count);
			for (int i = 0; i < crewManifest.Count; i++)
			{
				DynamicEntry dynamicEntry = crewManifestTemplate.Entries[i];
				var entry = dynamicEntry.GetComponent<GUI_PDAManifestTemplate>();
				CrewManifestEntry record = crewManifest[i];
				Occupation occupation = OccupationList.Instance.Get(record.JobType);
				entry.ReInit(record.Name, occupation != null ? occupation.DisplayName : "[Redacted]");
			}
		}

		private void ClearEntries()
		{
			crewManifestTemplate.Clear();
		}
	}
}
