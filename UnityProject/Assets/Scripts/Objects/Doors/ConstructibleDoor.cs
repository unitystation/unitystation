using System.Collections;
using System.Collections.Generic;
using Doors;
using Messages.Server;
using Messages.Server.SoundMessages;
using UnityEngine;

namespace Doors
{
	public class ConstructibleDoor : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public DoorAnimatorV2 DoorAnimatorV2;

		public bool Reinforced = false;

		private bool panelopen = false;

		public bool Panelopen => panelopen;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver) == false)
				return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			panelopen = !panelopen;
			if (panelopen)
			{
				DoorAnimatorV2.AddPanelOverlay();
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You unscrews the {gameObject.ExpensiveName()}'s cable panel.",
					$"{interaction.Performer.ExpensiveName()} unscrews {gameObject.ExpensiveName()}'s cable panel.");
			}
			else
			{
				DoorAnimatorV2.RemovePanelOverlay();
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You screw in the {gameObject.ExpensiveName()}'s cable panel.",
					$"{interaction.Performer.ExpensiveName()} screws in {gameObject.ExpensiveName()}'s cable panel.");

				//Force close net tab when panel is closed
				TabUpdateMessage.SendToPeepers(gameObject, NetTabType.HackingPanel, TabAction.Close);
			}


			AudioSourceParameters audioSourceParameters =
				new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver,
				interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
		}
	}
}