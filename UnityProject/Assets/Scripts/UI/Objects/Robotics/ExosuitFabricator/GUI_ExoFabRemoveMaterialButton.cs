using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabRemoveMaterialButton : NetButton
	{
		public int value = 5;
		public ItemTrait itemTrait;

		public override void ExecuteServer(ConnectedPlayer subject)
		{
			ServerMethod.Invoke();
		}
	}
}
