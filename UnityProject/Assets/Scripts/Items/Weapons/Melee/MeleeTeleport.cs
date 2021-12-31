using Systems.Teleport;
using UnityEngine;
using AddressableReferences;

namespace Weapons
{
	/// <summary>
	/// Adding this to a weapon randomly teleports the target on hit. Note: This script works almost identically to MeleeStun.cs with just a seperate OnHit effect.
	/// </summary>
	public class MeleeTeleport : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public bool avoidSpace;
		public bool avoidImpassable = true;

		[Tooltip("Min distance players could be teleported")]
		public int minTeleportDistance = 1;
		[Tooltip("Max distance players could be teleported")]
		public int maxTeleportDistance = 5;

		[Tooltip("How long you have to wait before teleporting another target. i.e Teleport Cooldown")]
		[SerializeField]
		private int delay = 5;
		/// <summary>
		/// if you can teleport a target
		/// </summary>
		private bool canTeleport = true;

		/// <summary>
		/// Sounds to play when teleporting someone
		/// </summary>
		[SerializeField] private AddressableAudioSource teleportSound = null;

		private int timer = 0;

		//Send only one message per second.
		private bool coolDownMessage;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return interaction.UsedObject == gameObject
				&& interaction.TargetObject.GetComponent<RegisterPlayer>();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			GameObject target = interaction.TargetObject;
			GameObject performer = interaction.Performer;

			// Direction for lerp
			Vector2 dir = (target.transform.position - performer.transform.position).normalized;

			WeaponNetworkActions wna = performer.GetComponent<WeaponNetworkActions>();

			// If we're on harm intent we deal damage!
			// Note: this has to be done before the teleport, otherwise the target may be moved out of range.
			if (interaction.Intent == Intent.Harm)
			{
				// Direction of attack towards the attack target.
				wna.ServerPerformMeleeAttack(target, dir, interaction.TargetBodyPart, LayerType.None);
			}

			RegisterPlayer registerPlayerVictim = target.GetComponent<RegisterPlayer>();

			// Teleport the victim. We check if there is a cooldown preventing the attacker from teleporting the victim.
			if (registerPlayerVictim && canTeleport)
			{
				// deactivates the teleport and makes you wait.
				if (delay != 0)
				{
					canTeleport = false;
					timer = delay;
				}

				TeleportUtils.ServerTeleportRandom(target, minTeleportDistance, maxTeleportDistance, avoidSpace, avoidImpassable);

				SoundManager.PlayNetworkedAtPos(teleportSound, target.transform.position, sourceObj: target.gameObject);

				// Special case: If we're off harm intent (only teleporting), we should still show the lerp (unless we're hitting ourselves).
				if (interaction.Intent != Intent.Harm && performer != target)
				{
					wna.RpcMeleeAttackLerp(dir, gameObject);
				}
			}
			else if (!canTeleport)
			{
				if (coolDownMessage) return;
				coolDownMessage = true;
				Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} is on cooldown.");
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(Timer, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Timer);
		}

		private void Timer()
		{
			if (timer == 0) return;

			timer--;

			coolDownMessage = false;

			if (timer == 0)
			{
				canTeleport = true;
			}
		}
	}
}