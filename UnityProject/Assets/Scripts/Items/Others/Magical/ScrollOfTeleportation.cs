using System.Collections;
using UnityEngine;
using Mirror;
using Systems.Spells.Wizard;
using ScriptableObjects.Systems.Spells;

namespace Items.Scrolls.TeleportScroll
{
	public class ScrollOfTeleportation : Scroll
	{
		private HasNetworkTabItem netTab;
		private TeleportSpell teleport;

		protected override void Awake()
		{
			base.Awake();
			netTab = GetComponent<HasNetworkTabItem>();
			teleport = GetComponent<TeleportSpell>();
		}

		public override bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (base.WillInteract(interaction, side) == false) return false;

			// If charges remain, return false to allow the HasNetworkTabItem component to take over.
			return !HasCharges;
		}

		public void TeleportTo(TeleportDestination destination)
		{
			var teleportingPlayer = netTab.LastInteractedPlayer();

			if (!HasChargesRemaining(teleportingPlayer)) return;

			if (teleport.IsBusy)
			{
				Chat.AddExamineMsgFromServer(teleportingPlayer, $"You are already teleporting!");
				return;
			}

			Transform spawnTransform = PlayerSpawn.GetSpawnForJob((JobType)destination);
			teleport.ServerTeleportWizard(teleportingPlayer, spawnTransform.position.CutToInt());

			SpellData teleportSpell = SpellList.Instance.Spells.Find(spell => spell.Name == "Teleport");

			SoundManager.PlayNetworkedAtPos(
					teleportSpell.CastSound, teleportingPlayer.Player().Script.WorldPos, sourceObj: teleportingPlayer);

			var incantation = $"{teleportSpell.InvocationMessage.Trim('!')} {destination.ToString().ToUpper()}!";
			Chat.AddChatMsgToChat(teleportingPlayer.Player(), incantation, ChatChannel.Local);

			ChargesRemaining--;
		}
	}

	// Use the JobType's spawnpoint as the destination.
	public enum TeleportDestination
	{
		Commons = JobType.ASSISTANT,
		Atmospherics = JobType.ATMOSTECH,
		Cargo = JobType.CARGOTECH,
		Medbay = JobType.DOCTOR,
		Engineering = JobType.ENGINEER,
		Science = JobType.SCIENTIST,
		Security = JobType.SECURITY_OFFICER
	}
}
