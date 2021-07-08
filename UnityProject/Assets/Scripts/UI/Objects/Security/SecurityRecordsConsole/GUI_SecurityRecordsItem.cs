using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Security;

namespace UI.Objects.Security
{
	/// <summary>
	/// SecurityRecords item is used inside EntriesPage.
	/// </summary>
	public class GUI_SecurityRecordsItem : DynamicEntry
	{
		private GUI_SecurityRecords securityRecordsTab;
		private SecurityRecord securityRecord;
		[SerializeField]
		private NetLabel recordNameText = null;
		[SerializeField]
		private NetLabel recordIdText = null;
		[SerializeField]
		private NetLabel recordRankText = null;
		[SerializeField]
		private NetLabel recordFingerprintsText = null;
		[SerializeField]
		private NetLabel recordStatusText = null;
		[SerializeField]
		private NetColorChanger recordBgColor = null;

		public void ReInit(SecurityRecord record, GUI_SecurityRecords recordsTab)
		{
			if (record == null)
			{
				Logger.Log("SecurityRecordItem: no record found, not doing init", Category.Machines);
				return;
			}
			securityRecord = record;
			securityRecordsTab = recordsTab;
			recordNameText.SetValueServer(record.EntryName);
			recordIdText.SetValueServer(record.ID);
			recordRankText.SetValueServer(record.Rank);
			recordFingerprintsText.SetValueServer(record.Fingerprints);
			recordStatusText.SetValueServer(record.Status.ToString());
			recordBgColor.SetValueServer(GetStatusColor(record.Status));
		}

		private Color GetStatusColor(SecurityStatus status)
		{
			switch (status)
			{
				case SecurityStatus.None:
					return DebugTools.HexToColor("424142");
				case SecurityStatus.Arrest:
					return DebugTools.HexToColor("C10000");
				case SecurityStatus.Criminal:
					return DebugTools.HexToColor("C17100");
				case SecurityStatus.Parole:
					return DebugTools.HexToColor("F57211");
			}
			return DebugTools.HexToColor("424142");
		}

		public void OpenRecord()
		{
			securityRecordsTab.OpenRecord(securityRecord);
		}
	}
}
