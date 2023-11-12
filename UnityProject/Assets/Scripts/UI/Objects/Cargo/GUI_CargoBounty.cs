using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;


namespace UI.Objects.Cargo
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
