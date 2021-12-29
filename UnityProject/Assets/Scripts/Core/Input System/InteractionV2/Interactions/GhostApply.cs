using UnityEngine;

namespace Systems.Interaction
{
	public class GhostApply : TargetedInteraction
	{
		public GhostApply(GameObject performer, GameObject usedObject, GameObject targetObject, Intent intent) : base(performer, usedObject, targetObject, intent)
		{
			this.Performer = performer;
			this.TargetObject = targetObject;
		}

		public static GhostApply ByClient(GameObject performer, GameObject usedObject, GameObject targetObject, Intent intent)
		{
			return new GhostApply(performer, usedObject, targetObject, intent);
		}
	}
}