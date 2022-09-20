using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
	public class ChangeLogEntryUI : MonoBehaviour
	{
		public TMP_Text text;

		public static class ChangelogIcons
		{
			public static string FIX => "";
			public static string BALANCE => "";
			public static string NEW => "";
			public static string IMPROVE => "";
			public static string UNKNOWN => "?";
		}

		public void SetEntry(ChangeLogEntry entryData)
		{
			text.text = $"{CatagoryGrabber(entryData.category)} - {entryData.description}\n\rAuthor: {entryData.author_username}";
		}

		private string CatagoryGrabber(string catagory)
		{
			switch (catagory)
			{
				case "New":
					return ChangelogIcons.NEW;
				case "Improvement":
					return ChangelogIcons.IMPROVE;
				case "Balance":
					return ChangelogIcons.BALANCE;
				case "Fix":
					return ChangelogIcons.FIX;
				default:
					return ChangelogIcons.UNKNOWN;
			}
		}
	}
}
