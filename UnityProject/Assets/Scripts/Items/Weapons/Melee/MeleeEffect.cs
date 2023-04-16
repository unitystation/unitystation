using Systems.Teleport;
using UnityEngine;
using AddressableReferences;
using NaughtyAttributes;
using Systems.Construction.Parts;

namespace Weapons
{
	/// <summary>
	/// Adding this to a weapon allows it to stun and/or randomly teleport targets on hit. 
	/// </summary>
	public class MeleeEffect : MonoBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		/// <summary>
		/// Sounds to play when striking someone
		/// </summary>
		[SerializeField] private AddressableAudioSource useSound = null;

		[Tooltip("How long you have to wait before triggering this weapons effect again.")]
		[SerializeField]
		[Space(10)]
		private int cooldown = 5;

		[Space(10)]
		[Tooltip("Does this weapon stun players on hit?")]
		public bool canStun = false;

		[ShowIf(nameof(canStun))]
		[Tooltip("How long to stun for (in seconds)")]
		public float stunTime = 0;

		[Space(10)]
		[Tooltip("Does this weapon teleport players on hit?")]
		public bool canTeleport = false;

		[ShowIf(nameof(canTeleport))]
		public bool avoidSpace;
		[ShowIf(nameof(canTeleport))]
		public bool avoidImpassable = true;

		[ShowIf(nameof(canTeleport))]
		[Range(0,15)]
		[Tooltip("Min distance players could be teleported")]
		public int minTeleportDistance = 1;

		[ShowIf(nameof(canTeleport))]
		[Range(0, 15)]
		[Tooltip("Max distance players could be teleported")]
		public int maxTeleportDistance = 5;

		/// <summary>
		/// if you can teleport and/or stun a target
		/// </summary>
		private bool canEffect = true;

		private int timer = 0;

		//Send only one message per second.
		private bool coolDownMessage;

		[Space(10)]
		[Tooltip(" Does this weapon rely on battery power to function?")]
		public bool hasBattery = false;

		[HideInInspector]
		public Battery Battery => batterySlot.Item != null ? batterySlot.Item.GetComponent<Battery>() : null;

		[ShowIf(nameof(hasBattery))]
		[Tooltip("How much power (in watts) is used per strike? High capacity powercells have a 10000 Watt capacity")]
		public int chargeUsage = 0;

		[ShowIf(nameof(hasBattery))]
		[Tooltip("What power cell should this weapon start with?")]
		public GameObject cellPrefab = null;

		[ShowIf(nameof(hasBattery))]
		[Tooltip("Can the power cell be removed via screwdriver?")]
		public bool allowScrewdriver = true;

		[HideInInspector]
		public ItemSlot batterySlot = null;

		public void Awake()
		{
			ItemStorage itemStorage = GetComponent<ItemStorage>();

			if (itemStorage != null && hasBattery)
			{
				batterySlot = itemStorage.GetIndexedItemSlot(0);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (cellPrefab != null)
			{
				GameObject cell = Spawn.ServerPrefab(cellPrefab).GameObject;
				Inventory.ServerAdd(cell, batterySlot, ReplacementStrategy.DespawnOther);
			}
		}

		#region handinteraction

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
			ToggleableEffect toggleableEffect = gameObject.GetComponent<ToggleableEffect>();

			// If we're on harm intent we deal damage!
			// Note: this has to be done before the teleport, otherwise the target may be moved out of range.
			if (interaction.Intent == Intent.Harm &&
				(toggleableEffect.CurrentWeaponState == ToggleableEffect.WeaponState.Off
				|| toggleableEffect.CurrentWeaponState == ToggleableEffect.WeaponState.NoCell))
			{
				// Direction of attack towards the attack target.
				wna.ServerPerformMeleeAttack(target, dir, interaction.TargetBodyPart, LayerType.None);
				return; //Only do damage to the target, do not do anything afterwards.
			}
			//If the thing is off on any intent, tell the player that it won't do anything.
			else if(toggleableEffect.CurrentWeaponState == ToggleableEffect.WeaponState.Off
				|| toggleableEffect.CurrentWeaponState == ToggleableEffect.WeaponState.NoCell)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You attempt to prod {interaction.TargetObject.ExpensiveName()} but the {gameObject.ExpensiveName()} was off!",
					$"{interaction.Performer.ExpensiveName()} prods {interaction.TargetObject.ExpensiveName()}, luckily the {gameObject.ExpensiveName()} was off!");
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Tap, gameObject.RegisterTile().WorldPosition);
				return;
			}

			if (canEffect && hasBattery)
			{
				if (Battery.Watts >= chargeUsage)
				{
					Battery.Watts -= chargeUsage;
				}
				else
				{
					if(toggleableEffect != null)
					{
						toggleableEffect.TurnOff();
					}

					timer = cooldown;
					canEffect = false;
				}
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
				if(hasBattery)
				{
					if(Battery.Watts >= chargeUsage)
					{
						Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} is on cooldown.");
					}
					else
					{
						Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} is out of power.");
					}
				}
				else
				{
					Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} is on cooldown.");
				}
			}
		}

		#endregion

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
