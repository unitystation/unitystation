using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions.Internal;
using US13.Player;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Core.Input_System.InteractionV2.Interactions
{
	/// <summary>
	/// Encapsulates all of the info needed for handling Ai interactions.
	/// </summary>
	public class AiActivate : TargetedInteraction
	{
		private ClickTypes clickType;

		public ClickTypes ClickType => clickType;

		public AiActivate(GameObject performer, GameObject usedObject, GameObject targetObject, Intent intent, Mind inMind, ClickTypes clickType) : base(performer, usedObject, targetObject, intent, inMind)
		{
			this.clickType = clickType;
		}



		public enum ClickTypes
		{
			AltClick,
			CtrlClick,
			ShiftClick,
			CtrlShiftClick,
			NormalClick
		}
	}
}
