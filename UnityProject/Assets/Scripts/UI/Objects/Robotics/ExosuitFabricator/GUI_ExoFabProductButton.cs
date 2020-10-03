using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Machines
{
	public class GUI_ExoFabProductButton : NetButton
	{
		[HideInInspector]
		public MachineProduct machineProduct;

		[HideInInspector]
		public string categoryName;

		public override void ExecuteServer(ConnectedPlayer subject)
		{
			ServerMethod.Invoke();
		}
	}
}
