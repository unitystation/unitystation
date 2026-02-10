using UnityEngine;
using US13.HealthV2;
using US13.Player;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Core.Input_System.InteractionV2.Interactions.Internal
{
	/// <summary>
	/// Abstract, only used internally for IF2 - should not be used in interactable components.
	/// Targeted interaction which targets a specific body part
	/// </summary>
	public abstract class BodyPartTargetedInteraction: TargetedInteraction
	{
		/// <summary>Body part being targeted.</summary>
		public BodyPartType TargetBodyPart { get; protected set; }

		/// <summary>
		///
		/// </summary>
		/// <param name="performer">The gameobject of the player performing the drop interaction</param>
		/// <param name="usedObject">Object that is being used</param>
		/// <param name="targetObject">Object that is being targeted</param>
		/// <param name="targetBodyPart">targeted body part</param>
		public BodyPartTargetedInteraction(GameObject performer, GameObject usedObject, GameObject targetObject, BodyPartType targetBodyPart, Intent intent, Mind inMind) :
			base(performer, usedObject, targetObject, intent, inMind)
		{
			TargetBodyPart = targetBodyPart;
		}
	}
}
