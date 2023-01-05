using UI.Core.NetUI;
using UnityEngine;
using Systems.Research;
using System.Collections.Generic;
using Chemistry;
using System.Text;

namespace UI.Objects.Research
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
