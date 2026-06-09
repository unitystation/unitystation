using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.UI.Core.ProgressBar;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Objects.Other
{
	public class PortableFlasher : FlasherBase, ICheckedInteractable<HandApply>
	{
		[SerializeField, SyncVar] private bool isWrenched = false;
		[SerializeField, SyncVar] private bool isOn = false;
		[SerializeField] private float progressBarTime = 4f;
		[SerializeField] private float timeInBetweenFlashes = 12f;

		private UniversalObjectPhysics objectBehaviour;

		private void Awake()
		{
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			if(CustomNetworkManager.IsServer == false) return;
			//Incase mappers want the portable flashers to be active on the map without someone setting it up
			if(isOn) UpdateManager.Add(FlashInRadius, timeInBetweenFlashes);
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null)
			{
				SwitchOffOn(interaction.PerformerPlayerScript);
				return;
			}
			if(interaction.HandObject.Item().HasTrait(CommonTraits.Instance.Wrench) == false) return;

			void DoWrenchInteraction()
			{
				WrenchInteraction(interaction.PerformerPlayerScript);
			}
			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Mop);
			var bar = StandardProgressAction.Create(cfg, DoWrenchInteraction).ServerStartProgress(interaction.Performer.AssumedWorldPosServer(), progressBarTime, interaction.Performer);
		}

		private void WrenchInteraction(PlayerScript wrenchHolder)
		{
			isWrenched = !isWrenched;
			//We inverse this to get the opposite of the wrench, so if its not wrenched; isPushable is true and vice versa
			objectBehaviour.SetIsNotPushable(isWrenched);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer());
			var status = !isWrenched ? "movable" : "immovable";
			Chat.AddActionMsgToChat(wrenchHolder.gameObject, $"The {gameObject.ExpensiveName()} is now {status}",
				$"{wrenchHolder.visibleName} uses the wrench on the {gameObject.ExpensiveName()}");
		}

		private void SwitchOffOn(PlayerScript switcher)
		{
			if (isWrenched == false && isOn == false)
			{
				Chat.AddExamineMsg(switcher.gameObject, "You need to wrench this down first before you can turn it on!");
				return;
			}

			isOn = !isOn;
			if (isOn)
			{
				UpdateManager.Add(FlashInRadius, timeInBetweenFlashes);
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE ,FlashInRadius);
			}
			var status = isOn ? "on" : "off";
			Chat.AddActionMsgToChat(gameObject, $"{switcher.visibleName} switches {status} the {gameObject.ExpensiveName()}.");
		}
	}
}