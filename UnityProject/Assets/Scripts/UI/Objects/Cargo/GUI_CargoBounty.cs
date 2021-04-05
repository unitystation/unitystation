using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		private CargoBounty cargoBounty;

		public NetLabel bountyDescription;

		public void SetValues(CargoBounty newCargoBounty)
		{
			cargoBounty = newCargoBounty;
			bountyDescription.SetValueServer($"{cargoBounty.Reward.ToString()} credits - {cargoBounty.Description}");
		}
	}
}
