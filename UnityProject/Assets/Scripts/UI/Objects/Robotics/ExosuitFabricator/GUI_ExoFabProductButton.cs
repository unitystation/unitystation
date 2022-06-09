using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

namespace Objects.Machines
{
	public class GUI_ExoFabProductButton : NetButton
	{
		[HideInInspector]
		public MachineProduct machineProduct;

		[HideInInspector]
		public string categoryName;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke();
		}
	}
}
