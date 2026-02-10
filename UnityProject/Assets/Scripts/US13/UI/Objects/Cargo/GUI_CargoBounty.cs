using UnityEngine;
using US13.Systems.Cargo;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		[SerializeField] private NetText_label bountyTitle;
		[SerializeField] private NetText_label InvisblebountyDescription;

		public void SetValues(CargoBounty cargoBounty)
		{
			bountyTitle.MasterSetValue($"{cargoBounty.Reward} credits - {cargoBounty.Title}");
			InvisblebountyDescription.MasterSetValue(cargoBounty.TooltipDescription);
		}
	}
}
