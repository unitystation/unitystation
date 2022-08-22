using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using UI.Core;

namespace UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		[SerializeField] private NetText_label bountyTitle;
		[SerializeField] private NetText_label InvisblebountyDescription;

		public void SetValues(CargoBounty cargoBounty)
		{
			bountyTitle.SetValueServer($"{cargoBounty.Reward} credits - {cargoBounty.Title}");
			InvisblebountyDescription.SetValueServer(cargoBounty.TooltipDescription);
		}
	}
}
