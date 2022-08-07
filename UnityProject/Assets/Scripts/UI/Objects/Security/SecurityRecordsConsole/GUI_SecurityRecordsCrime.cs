using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Security;

namespace UI.Objects.Security
{
	public class GUI_SecurityRecordsCrime : DynamicEntry
	{
		private SecurityRecordCrime crime;
		private GUI_SecurityRecordsEntryPage entryPage;
		[SerializeField]
		private NetText_label crimeText = null;
		[SerializeField]
		private NetText_label detailsText = null;
		[SerializeField]
		private NetText_label authorText = null;
		[SerializeField]
		private NetText_label timeText = null;

		public void ReInit(SecurityRecordCrime crimeToInit, GUI_SecurityRecordsEntryPage entryPageToInit)
		{
			crime = crimeToInit;
			entryPage = entryPageToInit;

			crimeText.SetValueServer(crime.Crime);
			detailsText.SetValueServer(crime.Details);
			authorText.SetValueServer(crime.Author);
			timeText.SetValueServer(crime.Time);
		}

		public void DeleteCrime()
		{
			entryPage.DeleteCrime(crime);
		}

		public void SetEditingField(NetText_label fieldToEdit)
		{
			entryPage.SetEditingField(fieldToEdit, crime);
		}

		public void OpenPopup(NetText_label fieldToEdit)
		{
			//Previously we set entryPage only server-side, but popup is opening client-side
			if (entryPage == null)
				entryPage = GetComponentInParent<GUI_SecurityRecordsEntryPage>();
			entryPage.OpenPopup(fieldToEdit);
		}
	}
}

namespace Objects.Security
{
	[Serializable]
	public class SecurityRecordCrime
	{
		public string Crime;
		public string Details;
		public string Author;
		public string Time;

		public SecurityRecordCrime()
		{
			Crime = "None";
			Details = "-";
			Author = "The law";
			Time = "12:00";
		}
	}
}
