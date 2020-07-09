using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAManifestTemplate : DynamicEntry
	{
		[SerializeField] private NetLabel playerName;

		[SerializeField] private NetLabel job;
		public void ReInit(string charName, string jobName)
		{
			playerName.Value = charName;
			job.Value = jobName;
		}
	}
}
