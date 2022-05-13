using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using UI.Core;

namespace UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		[SerializeField] private NetLabel bountyDescription;
		[SerializeField] private TooltipNetworked bountyTooltip;

		public void SetValues(CargoBounty cargoBounty)
		{
			bountyDescription.SetValueServer($"{cargoBounty.Reward} credits - {cargoBounty.Title}");
			bountyTooltip.TooltipText = cargoBounty.TooltipDescription;
		}
	}
}
