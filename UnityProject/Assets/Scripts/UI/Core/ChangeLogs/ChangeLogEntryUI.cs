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
			public const static string FIX => "";
			public const static string BALANCE => "";
			public const static string NEW => "";
			public const static string IMPROVE => "";
			public const static string UNKNOWN => "?";
		}

		public void SetEntry(ChangeLogEntry entryData)
		{
			text.text = $"{CatagoryGrabber(entryData.category)} - {entryData.description}\n\rAuthor: {entryData.author_username}";
		}

		private string CatagoryGrabber(string catagory)
		{
			switch (catagory)
			{
				case "NEW":
					return ChangelogIcons.NEW;
				case "IMPROVEMENT":
					return ChangelogIcons.IMPROVE;
				case "BALANCE":
					return ChangelogIcons.BALANCE;
				case "FIX":
					return ChangelogIcons.FIX;
				default:
					return ChangelogIcons.UNKNOWN;
			}
		}
	}
}
