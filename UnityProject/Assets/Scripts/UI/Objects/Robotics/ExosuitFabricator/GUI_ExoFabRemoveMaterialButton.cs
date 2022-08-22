using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabRemoveMaterialButton : NetButton
	{
		public int value = 5;
		public ItemTrait itemTrait;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke();
		}
	}
}
