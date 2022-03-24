using System;
using System.Collections;
using AddressableReferences;
using Mirror;
using Systems.Communications;
using UnityEngine;

namespace Items.Bureaucracy
{
	public class Megaphone : NetworkBehaviour, IChatInfluencer, ICheckedInteractable<HandApply>, IInteractable<HandActivate>
	{
		[SyncVar] private bool isOnCooldown = false;
		[SyncVar] private bool isOn = false;
		[SyncVar] private bool isEmagged = false;
		[SerializeField] private float cooldown = 10f;
		[SerializeField] private AddressableAudioSource megaphoneSound;

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

		public bool WillInfluenceChat()
		{
			return IsOnCooldown() == false && isOn == true && pickupable.ItemSlot != null;
		}

		private bool IsOnCooldown()
		{
			if (isEmagged) return false;
			if (isOnCooldown && pickupable.ItemSlot != null && pickupable.ItemSlot.Player != null)
			{
				Chat.AddExamineMsg(pickupable.ItemSlot.Player.gameObject, "The megaphone's internal capacitor is still recharging...");
			}
			return isOnCooldown;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			var modifiedMsg = chatToManipulate;
			modifiedMsg.VoiceLevel = isEmagged ? Loudness.EARRAPE : Loudness.MEGAPHONE;
			StartCoroutine(Cooldown());
			_ = SoundManager.PlayNetworkedAtPosAsync(megaphoneSound, gameObject.AssumedWorldPosServer());
			return modifiedMsg;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return isEmagged == false && DefaultWillInteract.Default(interaction, side) && pickupable.ItemSlot != null;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject == null ||
			    interaction.TargetObject.TryGetComponent<Emag>(out var mag) == false) return;
			if (mag.UseCharge(interaction)) isEmagged = true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			isOn = !isOn;
			var status = isOn ? "on" : "off";
			Chat.AddExamineMsg(interaction.Performer, $"you switch {status} the megaphone.");
		}
	}
}