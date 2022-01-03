using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoBounty : DynamicEntry
	{
		[SerializeField]
		private NetLabel bountyDescription;

		public void SetValues(CargoBounty cargoBounty)
		{
			bountyDescription.SetValueServer($"{cargoBounty.Reward} credits - {cargoBounty.Description}");
		}
	}
}
