using UnityEngine;
using US13.Managers;
using US13.Objects.Machines;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Research.Protolathe
{
	public class GUI_RDProProductButton : NetButton
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
