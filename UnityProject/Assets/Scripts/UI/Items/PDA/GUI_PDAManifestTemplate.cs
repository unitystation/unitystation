using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAManifestTemplate : DynamicEntry
	{
		[SerializeField] private NetLabel playerName = default;

		[SerializeField] private NetLabel job = default;
		public void ReInit(string charName, string jobName)
		{
			playerName.Value = charName;
			job.Value = jobName;
		}
	}
}
