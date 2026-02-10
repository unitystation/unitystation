using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Objects;
using US13.Systems.Occupations;
using US13.Systems.Spells;
using US13.Systems.Spells.Wizard;
using Util;

namespace US13.Items.Others.Magical
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
			PlayerInfo teleportingPlayer = GetLastReader();

			if (!HasChargesRemaining(teleportingPlayer.GameObject)) return;

			if (teleport.IsBusy)
			{
				Chat.AddExamineMsgFromServer(teleportingPlayer.GameObject, $"You are already teleporting!");
				return;
			}

			Transform spawnTransform = SpawnPoint.GetRandomPointForJob((JobType)destination);
			teleport.ServerTeleportWizard(teleportingPlayer, spawnTransform.position.CutToInt());

			SpellData teleportSpell = SpellList.Instance.Spells.Find(spell => spell.Name == "Teleport");

			SoundManager.PlayNetworkedAtPos(
					teleportSpell.CastSound, teleportingPlayer.Script.WorldPos, sourceObj: teleportingPlayer.GameObject);

			var incantation = $"{teleportSpell.InvocationMessage.Trim('!')} {destination.ToString().ToUpper()}!";
			Chat.AddChatMsgToChatServer(teleportingPlayer, incantation, ChatChannel.Local, Loudness.LOUD);

			ChargesRemaining--;
		}

		/// <summary>
		/// Gets the latest player to interact with tab.
		/// </summary>
		public PlayerInfo GetLastReader()
		{
			return netTab.LastInteractedPlayer().Player();
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
