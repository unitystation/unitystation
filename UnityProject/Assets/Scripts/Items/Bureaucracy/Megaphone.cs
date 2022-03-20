using System;
using System.Collections;
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
		[SerializeField] private float cooldown = 10f;

		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		private void OnDisable()
		{
			StopCoroutine(Cooldown());
		}

		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(cooldown);
			isOnCooldown = false;
		}

		public bool RunChecks()
		{
			return isOnCooldown == false && isOn == true && pickupable.ItemSlot != null;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			var modifiedMsg = chatToManipulate;
			modifiedMsg.VoiceLevel = isEmmaged ? Loudness.EARRAPE : Loudness.MEGAPHONE;
			StartCoroutine(Cooldown());
			return modifiedMsg;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return isEmmaged == false && DefaultWillInteract.Default(interaction, side) && pickupable.ItemSlot != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject == null ||
			    interaction.TargetObject.TryGetComponent<Emag>(out var mag) == false) return;
			if (mag.UseCharge(interaction)) isEmmaged = true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			isOn = !isOn;
			var status = isOn ? "on" : "off";
			Chat.AddExamineMsg(interaction.Performer, $"you switch {status} the megaphone.");
		}
	}
}