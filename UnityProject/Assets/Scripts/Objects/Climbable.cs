using UnityEngine;

namespace Objects
{
	public class Climbable : MonoBehaviour, ICheckedInteractable<MouseDrop>
	{
		public float ClimbTime = 1.0f;

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			
			PlayerSync playerSync;
			CustomNetTransform netTransform;
			if (interaction.UsedObject.TryGetComponent(out playerSync))
			{
				if (playerSync.IsMoving || playerSync.playerMove.IsBuckled) return false;

				// Do a sanity check to make sure someone isn't dropping the shadow from like 9000 tiles away.
				float mag = (interaction.TargetObject.transform.position - playerSync.ServerPosition).magnitude;
				if (mag > PlayerScript.interactionDistance) return false;
			}
			else if (interaction.UsedObject.TryGetComponent(out netTransform)) // Do the same check but for mouse draggable objects this time.
			{
				float mag = (interaction.TargetObject.transform.position - playerSync.ServerPosition).magnitude;
				if (mag > PlayerScript.interactionDistance) return false;
			}
			else // Not sure what this object is so assume that we can't interact with it at all.
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(MouseDrop interaction)
		{
			StandardProgressActionConfig cfg =
				new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			var x = StandardProgressAction.Create(cfg, () =>
			{
				PlayerScript playerScript;
				if (interaction.UsedObject.TryGetComponent(out playerScript))
				{
					playerScript.PlayerSync.SetPosition(this.gameObject.WorldPosServer());
				}
				else
				{
					var transformComp = interaction.UsedObject.GetComponent<CustomNetTransform>();
					if (transformComp != null)
					{
						transformComp.AppearAtPositionServer(this.gameObject.WorldPosServer());
					}
				}
			}).ServerStartProgress(interaction.UsedObject.RegisterTile(), ClimbTime, interaction.Performer);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin climbing onto the {interaction.TargetObject.gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} begins climbing onto the {interaction.TargetObject.gameObject.ExpensiveName()}...");
		}
	}
}

