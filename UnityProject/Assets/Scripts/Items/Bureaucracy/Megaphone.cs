using System;
using Mirror;
using Systems.Communications;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class Megaphone : NetworkBehaviour, IChatInfluncer, ICheckedInteractable<HandApply>, IInteractable<HandActivate>
	{
		[SyncVar] private bool isOnCooldown = false;
		[SyncVar] private bool isOn = false;
		[SyncVar] private bool isEmmaged = false;

		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		public bool RunChecks()
		{
			return isOnCooldown == false && isOn == true && pickupable.ItemSlot != null;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			var modifiedMsg = chatToManipulate;
			modifiedMsg.VoiceLevel = isEmmaged ? Loudness.EARRAPE : Loudness.MEGAPHONE;
			return modifiedMsg;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return isEmmaged == false && DefaultWillInteract.Default(interaction, side) && pickupable.ItemSlot != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != null && interaction.TargetObject.TryGetComponent<Emag>(out var mag))
			{
				if (mag.UseCharge(interaction)) isOn = true;
			}
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			isOn = !isOn;
			var status = isOn ? "on" : "off";
			Chat.AddExamineMsg(interaction.Performer, $"you switch {status} the megaphone.");
		}
	}
}