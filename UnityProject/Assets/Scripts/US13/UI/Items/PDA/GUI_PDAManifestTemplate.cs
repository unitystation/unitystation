using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Items.PDA
{
	public class GUI_PDAManifestTemplate : DynamicEntry
	{
		[SerializeField] private NetText_label playerName = default;

		[SerializeField] private NetText_label job = default;
		public void ReInit(string charName, string jobName)
		{
			playerName.MasterSetValue(charName);
			job.MasterSetValue(jobName);
		}
	}
}
