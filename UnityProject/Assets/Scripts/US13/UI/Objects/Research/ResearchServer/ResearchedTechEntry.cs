using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Research.ResearchServer
{
	public class ResearchedTechEntry : DynamicEntry
	{
		[SerializeField] private NetText_label techDescription;
		[SerializeField] private NetText_label techName;

		public void Initialise(string techName, string techDescription)
		{
			this.techName.MasterSetValue(techName);
			this.techDescription.MasterSetValue(techDescription);
		}
	}
}
