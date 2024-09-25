using Core;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Objects
{
	public class Climbable : MonoBehaviour, ICheckedInteractable<MouseDrop>
	{
		public float ClimbTime = 1.0f;

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (interaction.TargetObject == null) return false;
			if (interaction.UsedObject == null) return false;
			var targetObjectPos = interaction.TargetObject.transform.position;
			if (interaction.UsedObject.TryGetComponent<MovementSynchronisation>(out var playerSync))
			{
				if (playerSync.IsMoving || playerSync.IsBuckled) return false;
				// Do a sanity check to make sure someone isn't dropping the shadow from like 9000 tiles away.
				float mag = (targetObjectPos - playerSync.registerTile.WorldPosition ).magnitude;
				return mag <= PlayerScript.INTERACTION_DISTANCE;
			}
			// Do the same check but for mouse draggable objects this time.
			if (interaction.UsedObject.TryGetComponent<UniversalObjectPhysics>(out var UniversalObjectPhysics))
			{
				if (UniversalObjectPhysics.IsNotPushable) return false;
				float mag = (targetObjectPos - (UniversalObjectPhysics.transform.position)).magnitude;
				return mag <= PlayerScript.INTERACTION_DISTANCE;
			}
			return false;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			StandardProgressActionConfig cfg =
				new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, () =>
			{
				if (interaction.UsedObject.TryGetComponent<PlayerScript>(out var playerScript))
				{
					playerScript.PlayerSync.AppearAtWorldPositionServer(gameObject.AssumedWorldPosServer());
				}
				else
				{
					if (interaction.UsedObject.TryGetComponent<UniversalObjectPhysics>(out var UniversalObjectPhysics))
					{
						UniversalObjectPhysics.AppearAtWorldPositionServer(gameObject.AssumedWorldPosServer());
					}
				}
			}).ServerStartProgress(interaction.UsedObject.RegisterTile(), ClimbTime, interaction.Performer);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin climbing onto the {interaction.TargetObject.gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} begins climbing onto the {interaction.TargetObject.gameObject.ExpensiveName()}...");
		}
	}
}

