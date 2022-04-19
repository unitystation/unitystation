using AddressableReferences;
using Mirror;
using UnityEngine;

namespace Objects.Other
{
	public class PortableFlasher : FlasherBase, ICheckedInteractable<HandApply>
	{
		[SerializeField, SyncVar] private bool isWrenched = false;
		[SerializeField, SyncVar] private bool isOn = false;
		[SerializeField] private float progressBarTime;
		[SerializeField] private float timeInBetweenFlashes = 12f;

		private ObjectBehaviour objectBehaviour;


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
			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Mop);
			var bar = StandardProgressAction.Create(cfg, WrenchInteraction).ServerStartProgress(interaction.Performer.AssumedWorldPosServer(), progressBarTime, interaction.Performer);
		}

		private void WrenchInteraction()
		{
			isWrenched = !isWrenched;
			//We inverse this to get the opposite of the wrench, so if its not wrenched; isPushable is true and vice versa
			objectBehaviour.ServerSetPushable(!isWrenched);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Wrench, gameObject.AssumedWorldPosServer());
		}

		private void SwitchOffOn(PlayerScript switcher)
		{
			if (isWrenched == false && isOn == false)
			{
				Chat.AddExamineMsg(switcher.gameObject, "You need to wrench this first before you can turn it on!");
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
			Chat.AddLocalMsgToChat($"{switcher.visibleName} switches {status} the {gameObject.ExpensiveName()}", gameObject);
		}
	}
}