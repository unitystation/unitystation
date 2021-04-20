using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ChangeLogEntryUI : MonoBehaviour
	{
		public Text text;

		public void SetEntry(ChangeLogEntry entryData)
		{
			text.text = $"---------------------------------------------------\n\nDate: {entryData.date} \n\rAuthor: {entryData.author} " +
			 $"\n\r\n\rCommit: {entryData.commit} \n\r\n\rMessage: {entryData.message}";
		}
	}
}
