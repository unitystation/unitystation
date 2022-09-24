using TMPro;
using UI.Systems.ServerInfoPanel.Models;
using UnityEngine;

namespace UI.Systems.ServerInfoPanel
{
	public class ChangeEntry: MonoBehaviour
	{
		[field: SerializeField]
		public TMP_Text CategoryIcon { get; private set; }

		[field: SerializeField]
		public TMP_Text Description { get; private set; }

		[field: SerializeField]
		public TMP_Text Credits { get; private set; }

		private const string CREDITS_FORMAT = "Contributed by <color=\"white\"><link=\"{0}\"><b>{1}</b></link></color> in <color=\"white\"><link=\"{2}\"><b>PR #{3}</b></link></color>";

		public void SetChange(Change change)
		{
			CategoryIcon.text = IconConstants.ChangelogIcons[change.category];
			Description.text = change.description;
			Credits.text = string.Format(
				CREDITS_FORMAT,
				change.author_url,
				change.author_username,
				change.pr_url,
				change.pr_number);
		}
	}
}