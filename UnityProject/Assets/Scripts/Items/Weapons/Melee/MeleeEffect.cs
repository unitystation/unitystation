using Systems.Teleport;
using UnityEngine;
using AddressableReferences;

namespace Weapons
{
	/// <summary>
	/// Adding this to a weapon allows it to stun and/or randomly teleport targets on hit. 
	/// </summary>
	public class MeleeEffect : MonoBehaviour, ICheckedInteractable<HandApply>
	{

		[HideInInspector]
		[Tooltip("Does this weapon stun players on hit?")]
		public bool canStun = false;

		/// <summary>
		/// How long to stun for (in seconds)
		/// </summary>
		[HideInInspector]
		public float stunTime = 0;

		[HideInInspector]
		[Tooltip("Does this weapon teleport players on hit?")]
		public bool canTeleport = false;

		[HideInInspector]
		public bool avoidSpace;
		[HideInInspector]
		public bool avoidImpassable = true;

		[HideInInspector]
		[Tooltip("Min distance players could be teleported")]
		public int minTeleportDistance = 1;
		[HideInInspector]
		[Tooltip("Max distance players could be teleported")]
		public int maxTeleportDistance = 5;

		/// <summary>
		/// if you can teleport and/or stun a target
		/// </summary>
		private bool canEffect = true;

		/// <summary>
		/// Sounds to play when striking someone
		/// </summary>
		[SerializeField] private AddressableAudioSource useSound = null;

		[Tooltip("How long you have to wait before triggering this weapons effect again.")]
		[SerializeField]
		[Space(10)]
		private int cooldown = 5;

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

			// Teleport and stun the victim (if needed). We check if there is a cooldown preventing the attacker from effecting the victim.
			if (registerPlayerVictim && canEffect)
			{
				// deactivates the weapon and makes you wait.
				if (cooldown != 0)
				{
					canEffect = false;
					timer = cooldown;
				}

				if (canStun)
				{
					registerPlayerVictim.ServerStun(stunTime);
				}

				if (canTeleport)
				{
					TeleportUtils.ServerTeleportRandom(target, minTeleportDistance, maxTeleportDistance, avoidSpace, avoidImpassable);
				}

				SoundManager.PlayNetworkedAtPos(useSound, target.transform.position, sourceObj: target.gameObject);

				// Special case: If we're off harm intent (only teleporting and/or stunning), we should still show the lerp (unless we're hitting ourselves).
				if (interaction.Intent != Intent.Harm && performer != target)
				{
					wna.RpcMeleeAttackLerp(dir, gameObject);
				}
			}
			else if (!canEffect)
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
				canEffect = true;
			}
		}
	}
}
