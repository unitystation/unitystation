using UnityEngine;


namespace Systems.Interaction
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
