using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		public NetLabel bountyDescription;

		public void SetValues(CargoBounty cargoBounty)
		{
			bountyDescription.SetValueServer($"{cargoBounty.Reward.ToString()} credits - {cargoBounty.Description}");
		}
	}
}
