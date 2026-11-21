using UnityEngine;
using US13.Actions.V2;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI;
using US13.Player;

namespace US13.Systems.Antagonists
{
	/// <summary>
	/// Add to the core body part for a creature that may be polymorphed into.
	/// Handles possession and the returning to the original body once this form dies.
	/// </summary>
	public class PolymorphForm : BodyPartBehavior
	{
		private Mind parentMind;
		private PlayerScript originalBody;
		private PlayerScript currentBody;


		private bool destructiveExit = false; //Should this creature be deleted once a player leaves it? i.e is this a temporary construct?
		[SerializeField] private ActionButtonData ReturnToBodyButton;
		[SerializeField] private GameObject returnEffect = null;

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			currentBody = livingHealth?.playerScript;
			currentBody?.PlayerButtonedActions.RegisterNewAction(ReturnToBodyButton, ReturnToBody);
			livingHealth.OnDeath += DeathReturn;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			livingHealth.OnDeath -= DeathReturn;
			base.OnRemovedFromBody(livingHealth, source);
		}

		public void EnterBody(Mind mind, bool isDestructive)
		{
			if (parentMind != null) return; //Already possesed.

			destructiveExit = isDestructive;
			parentMind = mind;
			originalBody = mind.CurrentPlayScript;

			mind.SetPossessingObject(currentBody.gameObject);
			//Disable AI somehow
			mind.CurrentPlayScript.PlayerButtonedActions.RegisterNewAction(ReturnToBodyButton, ReturnToBody);
			var ais = mind.CurrentPlayScript.playerHealth.brain.RelatedPart.GetBodyPartBehaviors<BrainMobAI>();
			foreach(var ai in ais)
			{ai.OnBehaviorDisabled();}

		}

		//On Death return to original body, but stunned
		private void DeathReturn()
		{
			if (originalBody == false) return;
			originalBody.RegisterPlayer.ServerStun(5.0f, false, false, true); //punish player for dying in alternative form
			ReturnToBody(Vector2.zero);
		}

		private void ReturnToBody(Vector2 clickedPosition)
		{
			if (originalBody == false || currentBody == false) return;

			if(returnEffect) Spawn.ServerPrefab(returnEffect, currentBody.ObjectPhysics.OfficialPosition);

			currentBody?.PlayerButtonedActions.ServerRemoveAction(ReturnToBodyButton.ID);

			//Posses OG body
			originalBody.ObjectPhysics.AppearAtWorldPositionServer(currentBody.ObjectPhysics.OfficialPosition);
			parentMind.SetPossessingObject(originalBody.gameObject);
			parentMind = null;

			originalBody.playerHealth.ShouldPerformHealthUpdates = true;

			//Destroy this
			if (destructiveExit == false) return;
			_ = Despawn.ServerSingle(currentBody.gameObject);
		}
	}
}
