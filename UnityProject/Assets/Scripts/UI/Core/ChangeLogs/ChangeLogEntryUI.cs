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

		public void SetEntry(ChangeLogEntry entryData)
		{
			text.text = $"Date: {entryData.date_added} \n\rAuthor: {entryData.author_username} " +
			            $"\n\r\n\rCommit: {entryData.pr_number} \n\r\n\rMessage: {entryData.description}";
		}
	}
}
