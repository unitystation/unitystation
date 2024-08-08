﻿﻿﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;
using AddressableReferences;
using HealthV2;
using Logs;
using Messages.Server.SoundMessages;
using Weapons.Projectiles;
using NaughtyAttributes;
using Player;
using Weapons.WeaponAttachments;

//TODO: All of this needs to be fixed:
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Code cleanup:
		// - Update xml comments
		// - Remove code comments that are no longer relevant
		// - Add new code comments
		// - Redo logs, also re-add the logs that were removed when shotqueue was ripped out but probably shouldnt have been
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Feature fixes/implementation:
		// - Reimplement burstfire
		// - Redo mag/pin spawning behaviour
		// - Redo gun init behaviour
		// - Redo recoil implementation from shotqueue removal
		// - Redo shot cooldown behaviour from shotqueue removal, new impl needs to support things for burstfire
		// - Fix the ability to shoot over crit players and dead bodies
		// - Fix or Redo mag behaviour thats no longer needed or broken from shotqueue removal
		// - Update the way progress bars are done for firing pins
		// - Many weapons still dont have their projectile behaviours done
		// - Consider making recoil differ based on projectile
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// UX:
		// - Redo examine messages
		// - Redo action messages and add more of them
		// - Consider adding hovertooltip messages
		// - Consider adding mag retention reloads (probably not)
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Balance:
		// - Do a balance pass on all firearms after the above problems have been fixed

		// Dont make this list any longer, please...

namespace Weapons
{
	/// <summary>
	///  Allows an object to behave like a gun and fire shots. Server authoritative.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	[RequireComponent(typeof(ItemStorage))]
	public class Gun : NetworkBehaviour, ICheckedInteractable<AimApply>, ICheckedInteractable<HandActivate>,
		ICheckedInteractable<InventoryApply>, ICheckedInteractable<ContextMenuApply>, IRightClickable, IServerInventoryMove, IServerSpawn, IExaminable, ISuicide
	{
		[Header("Weapon Config")]

		public bool AllowSuicide;

		[SerializeField, Tooltip("The firing pin initally inside the gun")]
		private GameObject pinPrefab = null;

		[SerializeField] protected bool allowPinSwap = true;

		[Tooltip("The time in seconds before this weapon can fire again.")]
		public float FireDelay = 0.5f;

		[SerializeField, SyncVar(hook = nameof(SyncIsSuppressed)), Tooltip("If the gun displays a shooter message")]
		private bool isSuppressed;

		public bool IsSuppressed => isSuppressed;

		[Tooltip("Firemode of this weapon")] public WeaponType WeaponType;

		[HorizontalLine]

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[Header("Recoil Config")]

		//TODO connect these with the actual shooting of a projectile
		[Tooltip("The max recoil angle this weapon can reach with sustained fire")]
		public float MaxRecoilVariance;

		//This needs to be moved to projectiles
		[Tooltip("The speed of the projectile")]
		public int ProjectileVelocity;

		[Tooltip("Describes the recoil behavior of the camera when this gun is fired"),
		 SyncVar(hook = nameof(SyncCameraRecoilConfig))]
		public CameraRecoilConfig CameraRecoilConfig;

		[HorizontalLine]

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[Header("Mag/Ammo Config")]

		[SerializeField, Tooltip("Mag prefab to be spawned within on roundstart")]
		protected GameObject ammoPrefab = null;

		[FormerlySerializedAs("AmmoType"), Tooltip("The category of ammo this weapon can fire")]
		public AmmoType ammoType;

		[SerializeField, Tooltip("Optional casing override, defaults to standard casing when null")]
		private GameObject casingPrefabOverride = null;

		[Tooltip("If the weapon should spawn casings ")]
		public bool SpawnsCasing = true;

		[HideIf(nameof(SmartGun)), Tooltip("Enables internal mag behaviours (for things like revolvers or shotguns)")]
		public bool MagInternal = false;

		[HideIf(nameof(MagInternal)), Tooltip("If the gun should eject an empty mag automatically")]
		public bool SmartGun = false;

		[SerializeField, Tooltip("If the player is allowed to remove a loaded mag")]
		private bool allowMagazineRemoval = true;

		[HorizontalLine]

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[Header("Attachment Config")]

		[SerializeField, Tooltip("List of attachments to spawn on the weapon")]
		private List<GameObject> attachmentPrefabs = default;
		[SerializeField, EnumFlags] public AttachmentType allowedAttachments;

		[HorizontalLine]

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		[Header("Addressable Audio")]

		public AddressableAudioSource loadMagSound;
		public AddressableAudioSource unloadMagSound;
		public AddressableAudioSource FiringSoundA = null;
		public AddressableAudioSource SuppressedSoundA;
		public AddressableAudioSource DryFireSound;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		//server-side object indicating the player holding the weapon (null if none)
		protected GameObject ServerHolder;
		private RegisterTile shooterRegisterTile;

		/// <summary>
		/// The current magazine for this weapon, null means empty
		/// </summary>
		public MagazineBehaviour CurrentMagazine =>
			magSlot.Item != null ? magSlot.Item.GetComponent<MagazineBehaviour>() : null;

		/// <summary>
		/// The firing pin currently inside the weapon
		/// </summary>
		public PinBase FiringPin =>
			pinSlot.Item != null ? pinSlot.Item.GetComponent<PinBase>() : null;

		protected const float PinRemoveTime = 10f;

		protected StandardProgressActionConfig ProgressConfig
			= new(StandardProgressActionType.ItemTransfer);

		/// <summary>
		/// The the current recoil variance this weapon has reached
		/// </summary>
		[NonSerialized] public float CurrentRecoilVariance;

		/// <summary>
		/// If we are currently waiting on a shot cooldown to elapse
		/// </summary>
		[NonSerialized] public bool ShotCooldown = false;

		[ReadOnly] public ItemSlot magSlot;
		[ReadOnly] private ItemSlot pinSlot;

		private ItemStorage itemStorage;

		private readonly List<WeaponAttachment> weaponAttachments = new();
		private readonly List<ItemSlot> weaponAttachmentSlots = new();

		#region Init Logic

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			magSlot = itemStorage.GetIndexedItemSlot(0);
			pinSlot = itemStorage.GetIndexedItemSlot(1);

			var slots = itemStorage.GetItemSlots().ToList();
			for (int i = 0; i < slots.Count; i++)
			{
				if (i < 1)
				{
					continue;
				}
				weaponAttachmentSlots.Add(itemStorage.GetIndexedItemSlot(i));
			}

			if (pinSlot == null || magSlot == null || itemStorage == null)
			{
				Loggy.LogWarning($"{gameObject.name} missing components, may cause issues", Category.Firearms);
			}
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			if (MagInternal)
			{
				//automatic ejection always disabled
				SmartGun = false;
				//ejecting an internal mag should never be allowed
				allowMagazineRemoval = false;
			}

			//Default recoil if one has not been set already
			if (CameraRecoilConfig == null || CameraRecoilConfig.Distance == 0f)
			{
				var recoil = new CameraRecoilConfig
				{
					Distance = 0.2f,
					RecoilDuration = 0.05f,
					RecoveryDuration = 0.6f
				};
				SyncCameraRecoilConfig(CameraRecoilConfig, recoil);
			}

			if (ammoPrefab == null)
			{
				Loggy.LogError($"{gameObject.name} magazine prefab was null, cannot auto-populate.",
					Category.Firearms);
				return;
			}

			//populate with a full mag on spawn
			Loggy.LogTraceFormat("Auto-populate magazine for {0}", Category.Firearms, name);
			Inventory.ServerAdd(Spawn.ServerPrefab(ammoPrefab).GameObject, magSlot);

			if (pinPrefab == null)
			{
				Loggy.LogError($"{gameObject.name} firing pin prefab was null, cannot auto-populate.",
					Category.Firearms);
				return;
			}

			Inventory.ServerAdd(Spawn.ServerPrefab(pinPrefab).GameObject, pinSlot);
			FiringPin.gunComp = this;

			foreach (var prefab in attachmentPrefabs)
			{
				if (prefab == null)
				{
					Loggy.LogError($"{gameObject.name} has null prefab in attachmentPrefab list", Category.Firearms);
					continue;
				}

				var slot = weaponAttachmentSlots.FirstOrDefault(slot => slot.Item == null);

				if (slot == null)
				{
					Loggy.LogError($"{gameObject.name} had no free attachment slot to add {prefab.name}", Category.Firearms);
					continue;
				}

				Inventory.ServerAdd(Spawn.ServerPrefab(prefab).GameObject, slot);
				var attachment = slot.ItemObject.GetComponent<WeaponAttachment>();
				weaponAttachments.Add(attachment);
				attachment.AttachBehaviour(this);
			}
		}

		public virtual void OnInventoryMoveServer(InventoryMove info)
		{
			if (gameObject != info.MovedObject.gameObject) return;

			if (info.ToPlayer != null)
			{
				ServerHolder = info.ToPlayer.gameObject;
				shooterRegisterTile = ServerHolder.GetComponent<RegisterTile>();
			}
			else
			{
				ServerHolder = null;
				shooterRegisterTile = null;
			}
		}

		#endregion

		#region HandActivate

		public virtual bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//try ejecting the mag if external
			if (CurrentMagazine != null && allowMagazineRemoval && !MagInternal)
			{
				return true;
			}

			return false;
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			if (CurrentMagazine != null && allowMagazineRemoval && !MagInternal)
			{
				ServerUnloadMagazine();
			}
		}

		#endregion

		#region InventoryApply

		public virtual bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only reload if the gun is the target and item being used on us is in hand slot
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponAttachable) ||
				    Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) ||
				    Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin))
				{
					return true;
				}
				else if (interaction.UsedObject != null)
				{
					MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
					if (mag)
					{
						return CanReload(mag.gameObject);
					}
				}
			}

			return false;
		}

		public virtual void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
			{
				if (interaction.UsedObject != null)
				{
					MagazineBehaviour mag = interaction.UsedObject.GetComponent<MagazineBehaviour>();
					if (mag)
					{
						ServerReloadMagazine(mag.gameObject);
						return;
					}

					if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.WeaponAttachable))
					{
						AttachmentInteraction(interaction);
						return;
					}

					PinInteraction(interaction);
				}
			}
		}

		protected void AttachmentInteraction(InventoryApply interaction)
		{
			if (interaction.UsedObject.TryGetComponent<WeaponAttachment>(out var attachment))
			{
				//Check theres a free slot, check the attachment is allowed on this weapon
				//Check if either duplicate attachments are allowed or no existing attachments are of the same type
				//Call the attachments check function and then see if the attachment gets added to the weapon
				var slot = weaponAttachmentSlots.FirstOrDefault(slot => slot.Item == null);
				if (slot != null && allowedAttachments.HasFlag(attachment.AttachmentType) &&
					(attachment.AllowDuplicateAttachments || weaponAttachments.Any(att => att.AttachmentType.Equals(attachment.AttachmentType)) == false) &&
					attachment.AttachCheck(this) && Inventory.ServerTransfer(interaction.FromSlot, slot))
				{
					weaponAttachments.Add(attachment);
					attachment.AttachBehaviour(this);
					Chat.AddExamineMsgFromServer(ServerHolder, $"You attach the {interaction.UsedObject.ExpensiveName()} onto the {gameObject.ExpensiveName()}");
				}
				else
				{
					Chat.AddExamineMsgFromServer(ServerHolder, $"The {interaction.UsedObject.ExpensiveName()} won't fit on the {gameObject.ExpensiveName()}");
				}
			}
		}

		protected void PinInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wirecutter) && allowPinSwap)
			{
				PinRemoval(interaction);
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.FiringPin) && allowPinSwap)
			{
				PinAddition(interaction);
			}
		}

		/// <summary>
		/// method that handles inserting a firing pin into a firearm
		/// </summary>
		private void PinAddition(InventoryApply interaction)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You insert the {interaction.UsedObject.gameObject.ExpensiveName()} into {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} inserts the {interaction.UsedObject.gameObject.ExpensiveName()} into {gameObject.ExpensiveName()}.");
			Inventory.ServerTransfer(interaction.FromSlot, pinSlot);
			FiringPin.gunComp = this;
		}

		/// <summary>
		/// method that handles removal of a firing pin
		/// </summary>
		private void PinRemoval(InventoryApply interaction)
		{
			void ProgressFinishAction()
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You remove the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} removes the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}.");

				FiringPin.gunComp = null;

				Inventory.ServerDrop(pinSlot);
			}

			var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
				.ServerStartProgress(interaction.Performer.RegisterTile(), PinRemoveTime, interaction.Performer);

			if (bar != null)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You begin removing the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} begins removing the {FiringPin.gameObject.ExpensiveName()} from {gameObject.ExpensiveName()}.");

				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.WireCutter,
					interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: ServerHolder);
			}
		}

		#endregion

		#region ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, null), NetworkSide.Client)) return result;

			foreach (var attachment in weaponAttachments)
			{
				//If an attachment is already stored and we dont have the flag, assume its intended to be iremovable
				if (allowedAttachments.HasFlag(attachment.AttachmentType))
				{
					var interaction = ContextMenuApply.ByLocalPlayer(gameObject, attachment.InteractionKey);
					result.AddElement(attachment.InteractionKey, () => ContextMenuOptionClicked(interaction));
				}
			}
			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			if (ServerHolder != interaction.Performer) return;

			foreach (var attachment in weaponAttachments)
			{
				if (interaction.RequestedOption == attachment.InteractionKey)
				{
					if (attachment.DetachCheck(this) && TransferHandOrFloor(interaction, itemStorage.GetSlotFromItem(attachment.gameObject)))
					{
						weaponAttachments.Remove(attachment);
						attachment.DetachBehaviour(this);
						Chat.AddExamineMsgFromServer(ServerHolder, $"You detach the {attachment.gameObject.ExpensiveName()} from the {gameObject.ExpensiveName()}");
						return;
					}
				}
			}
		}

		private bool TransferHandOrFloor(ContextMenuApply interaction, ItemSlot targetslot)
		{
			ItemSlot hand = interaction.PerformerPlayerScript.DynamicItemStorage.GetBestHand();
			if (Inventory.ServerTransfer(targetslot, hand) == false)
			{
				return Inventory.ServerDrop(targetslot);
			}
			else
			{
				return true;
			}
		}

		#endregion

		#region AimApply

		public virtual bool WillInteract(AimApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			//Melee behaviour for things like bayonets
			if (interaction.Intent == Intent.Harm && BayonetCheck(interaction.Performer.RegisterTile().LocalPosition.To2(), interaction.TargetPosition))
			{
				return false;
			}

			if (CurrentMagazine == null)
			{
				PlayEmptySfx();
				if (side == NetworkSide.Server)
				{
					Loggy.LogTrace("Server rejected shot - No magazine being loaded", Category.Firearms);
				}

				return false;
			}

			if (FiringPin == null)
			{
				if (interaction.Performer == PlayerManager.LocalPlayerObject)
				{
					Chat.AddExamineMsgToClient("The " + gameObject.ExpensiveName() +
					                           "'s trigger is locked. It doesn't have a firing pin installed!");
				}

				Loggy.LogTrace("Rejected shot - no firing pin", Category.Firearms);
				return false;
			}


			if (ShotCooldown == false)
			{

				if (interaction.MouseButtonState == MouseButtonState.PRESS)
				{
					return true;
				}
				else
				{
					//being held, only can shoot if this is an automatic
					return WeaponType == WeaponType.FullyAutomatic;
				}
			}

			return false;
		}

		//.magnitude leaves out small area in the outer corner of tiles diagonal to the player pos
		//Doing it this way prevents that
		private bool BayonetCheck(Vector2 playerPos, Vector2 targetPos)
		{
			const float checkRange = 1.5f;
			if (playerPos.x >= targetPos.x - checkRange  && playerPos.x <= targetPos.x + checkRange)
			{
				if (playerPos.y >= targetPos.y - checkRange && playerPos.y <= targetPos.y + checkRange)
				{
					return true;
				}
			}
			return false;
		}

		private IEnumerator DelayGun()
		{
			ShotCooldown = true;
			yield return WaitFor.Seconds(FireDelay);
			ShotCooldown = false;
		}

		public virtual void ServerPerformInteraction(AimApply interaction)
		{
			if (CurrentMagazine.ServerAmmoRemains <= 0)
			{
				if (SmartGun && allowMagazineRemoval) // smartGun is forced off when using an internal magazine
				{
					ServerUnloadMagazine();
					OutOfAmmoSfx();
				}
				else
				{
					PlayEmptySfx();
				}

				StartCoroutine(DelayGun());
				return;
			}

			//do we need to check if this is a suicide (want to avoid the check because it involves a raycast).
			//case 1 - we are beginning a new shot, need to see if we are shooting ourselves
			//case 2 - we are firing an automatic and are currently shooting ourselves, need to see if we moused off
			//	ourselves.
			var isSuicide = false;
			if (interaction.MouseButtonState == MouseButtonState.PRESS ||
			    (WeaponType != WeaponType.SemiAutomatic && AllowSuicide))
			{
				if (Manager3D.Is3D == false)
				{
					isSuicide = interaction.IsAimingAtSelf;
					AllowSuicide = isSuicide;
				}
			}

			if (FiringPin != null)
			{
				FiringPin.ServerBehaviour(interaction, isSuicide);
			}

			if (interaction.Intent == Intent.Harm && interaction.UsedObject == gameObject)
			{
				List<ItemSlot> hands = interaction.PerformerPlayerScript.DynamicItemStorage.GetHandSlots();
				hands.Remove(interaction.PerformerPlayerScript.DynamicItemStorage.GetActiveHandSlot());
				foreach (var hand in hands)
				{
					if (hand.ItemObject != null && hand.ItemObject.TryGetComponent<Gun>(out var gun)
						&& gun.WillInteract(interaction, NetworkSide.Server))
					{
						gun.ServerPerformInteraction(interaction);
					}
				}
			}
		}

		#endregion

		public virtual string Examine(Vector3 pos)
		{
			StringBuilder exam = new StringBuilder();
			exam.AppendLine($"{WeaponType} - Fires {ammoType.ToString().Replace("_", "")} ammunition")
				.AppendLine(CurrentMagazine != null
					? $"{CurrentMagazine.ServerAmmoRemains} rounds loaded"
					: "It's empty!")
				.AppendLine(FiringPin != null
					? $"It has a {FiringPin.gameObject.ExpensiveName()} installed"
					: "It doesn't have a firing pin installed, it won't fire")
				.AppendLine(allowedAttachments != 0
				? $"It is compatible with {FormatAttachmentString()} attachments"
					: "It cannot use any attachments.");
			return exam.ToString();
		}

		//allowedAttachments.ToString() returns something similar to "foo, bar, baz"
		//this replaces the last instance of a comma (if there is one) with 'and', e.g "foo, bar and baz"
		public string FormatAttachmentString()
		{
			var attachRaw = allowedAttachments.ToString();
			var indexLast = attachRaw.LastIndexOf(',');
			return indexLast != -1 ? attachRaw.Remove(indexLast, 1).Insert(indexLast, " and") : attachRaw;
		}

		#region Weapon Firing Mechanism

		/// <summary>
		/// Handles validation, spawning projectiles, recoil, physics kickback and spawning casings
		/// Calling this directly will bypass firing pin validation, FiringPin.ServerBehaviour should generally be used instead
		/// </summary>
		/// <param name="shotBy">gameobject of the player performing the shot</param>
		/// <param name="target">normalized target vector(actual trajectory will differ due to accuracy)</param>
		/// <param name="damageZone">targeted body part</param>
		/// <param name="isSuicideShot">if this is a suicide shot</param>
		[Server]
		public void ServerShoot(GameObject shotBy, Vector2 target,
			BodyPartType damageZone, bool isSuicideShot)
		{
			//don't process the shot if the player is no longer able to interact
			PlayerScript shooterScript = shotBy.GetComponent<PlayerScript>();
			if (!Validations.CanInteract(shooterScript, NetworkSide.Server))
			{
				Loggy.LogTrace("Server rejected shot: shooter cannot interact", Category.Firearms);
				return;
			}

			// check if we can still input and are not ghost, blob, etc
			MovementSynchronisation shooterMovementSync = shotBy.GetComponent<MovementSynchronisation>();
			if (shooterMovementSync.AllowInput == false || shooterScript.IsNormal == false)
			{
				Loggy.Log("A player tried to shoot when not allowed or when they were a ghost.", Category.Exploits);
				Loggy.LogWarning("A shot was attempted when shooter is a ghost or is not allowed to shoot.",
					Category.Firearms);
				return;
			}


			if (CurrentMagazine == null || CurrentMagazine.ServerAmmoRemains <= 0 ||
			    CurrentMagazine.containedBullets[0] == null)
			{
				Loggy.LogTrace("Player tried to shoot when there was no ammo.", Category.Exploits);
				Loggy.LogWarning("A shot was attempted when there is no ammo.", Category.Firearms);
				return;
			}

			if (ShotCooldown)
			{
				Loggy.LogTrace("Player tried to shoot too fast.", Category.Exploits);
				Loggy.LogWarning("Shot attempted to fire whilst still cooling down.",
					Category.Exploits);
				return;
			}

			GameObject toShoot = CurrentMagazine.containedBullets[0];
			int quantity = CurrentMagazine.containedProjectilesFired[0];

			if (toShoot == null)
			{
				Loggy.LogError("Shot was attempted but no projectile or quantity was found to use", Category.Firearms);
				return;
			}

			var finalDirection = ApplyRecoil(target);
			//perform the actual server side shooting, creating the bullet that does actual damage
			ServerSpawnShots(shotBy, finalDirection, damageZone, isSuicideShot, toShoot,
				quantity);

			StartCoroutine(DelayGun());
			//trigger a hotspot caused by gun firing
			shooterRegisterTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(
				shooterMovementSync.gameObject.TileWorldPosition(), 500);

			if (isSuppressed == false && isSuicideShot == false)
			{
				Chat.AddActionMsgToChat(ServerHolder,
					$"You fire your {gameObject.ExpensiveName()}",
					$"{ServerHolder.ExpensiveName()} fires their {gameObject.ExpensiveName()}");
			}

			//kickback
			shooterScript.ObjectPhysics.NewtonianPush((-finalDirection).NormalizeToInt(), 1);

			if (SpawnsCasing)
			{
				if (casingPrefabOverride == null)
				{
					//no casing override set, use normal casing prefab
					casingPrefabOverride = CustomNetworkManager.Instance.GetSpawnablePrefabFromName("BulletCasing");
				}

				Spawn.ServerPrefab(casingPrefabOverride, shooterScript.transform.position,
					shooterScript.transform.parent);
			}
		}

		/// <summary>
		/// Spawns projectile(s), and handles firing sfx, additional recoil for next shot on automatic weapons and muzzle flash
		/// </summary>
		/// <param name="shooter">gameobject of the shooter</param>
		/// <param name="finalDirection">direction the shot should travel (accuracy deviation should already be factored into this)</param>
		/// <param name="damageZone">targeted damage zone</param>
		/// <param name="isSuicideShot">if this is a suicide shot (aimed at shooter)</param>
		/// <param name="projectile">prefab of the projectile that should be spawned</param>
		/// <param name="quantity">the amount of projectiles to spawn when spawning the shots</param>
		private void ServerSpawnShots(GameObject shooter, Vector2 finalDirection,
			BodyPartType damageZone, bool isSuicideShot, GameObject projectile, int quantity)
		{
			if (!MatrixManager.IsInitialized) return;

			//if this is our gun (or server), last check to ensure we really can shoot
			if (isServer || PlayerManager.LocalPlayerObject == shooter)
			{
				if (CurrentMagazine.ClientAmmoRemains <= 0)
				{
					if (isServer)
					{
						Loggy.LogTrace("Server rejected shot - out of ammo", Category.Firearms);
					}

					return;
				}

				CurrentMagazine.ExpendAmmo();
			}
			//TODO: If this is not our gun, simply display the shot, don't run any other logic

			//add additional recoil after shooting for the next round
			AppendRecoil();


			if (isSuicideShot)
			{
				GameObject newprojectile = Spawn.ServerPrefab(projectile,
					shooter.transform.position, parent: shooter.transform.parent).GameObject;
				Projectile projectileComponent = newprojectile.GetComponent<Projectile>();
				projectileComponent.Suicide(shooter, this, damageZone);
			}
			else
			{
				for (int n = 0; n < quantity; n++)
				{
					GameObject newprojectile = Spawn.ServerPrefab(projectile,
						shooter.transform.position, parent: shooter.transform.parent).GameObject;
					Projectile projectileComponent = newprojectile.GetComponent<Projectile>();
					Vector2 finalDirectionOverride = CalcProjectileDirections(finalDirection, n);
					projectileComponent.Shoot(finalDirectionOverride, shooter, this, damageZone);
				}
			}

			if (isSuppressed && SuppressedSoundA != null)
			{
				SoundManager.PlayNetworkedAtPos(SuppressedSoundA, shooter.transform.position);
			}
			else
			{
				SoundManager.PlayNetworkedAtPos(FiringSoundA, shooter.transform.position);
			}

			var identity = shooter.GetComponent<NetworkIdentity>();
			RPCShowMuzzleFlash(identity);
			if (identity.OrNull()?.connectionToClient != null)
			{
				if (isServer && shooter == PlayerManager.LocalPlayerObject)
				{
					Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
				}

				RPCShowRecoil(identity.connectionToClient, finalDirection);
			}
		}

		[TargetRpc]
		public void RPCShowRecoil(NetworkConnection target, Vector2 finalDirection)
		{
			Camera2DFollow.followControl.Recoil(-finalDirection, CameraRecoilConfig);
		}

		[ClientRpc]
		public void RPCShowMuzzleFlash(NetworkIdentity target)
		{
			target.GetComponent<PlayerSprites>().ShowMuzzleFlash();
		}

		private Vector2 CalcProjectileDirections(Vector2 direction, int iteration)
		{
			if (iteration == 0) return direction;

			//This is for shotgun spread and similar multi-projectile weapons
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			float angleVariance = iteration / 1f;
			float angleDeviation;
			if (iteration % 2 == 0)
			{
				angleDeviation = angleVariance; //even
			}
			else
			{
				angleDeviation = -angleVariance; //odd
			}

			float newAngle = (angle + angleDeviation) * Mathf.Deg2Rad;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		#endregion

		#region Weapon Loading and Unloading

		/// <summary>
		/// clientside method that checks if the player can reload
		/// </summary>
		/// <param name="ammo">gameobject of the magazine</param>
		private bool CanReload(GameObject ammo)
		{
			MagazineBehaviour magazine = ammo.GetComponent<MagazineBehaviour>();
			if (CurrentMagazine == null || (MagInternal && magazine.magType == MagType.Clip))
			{
				// If the item used on the gun is a magazine, check ammo type and reload
				if (ammoType == magazine.ammoType)
				{
					return true;
				}
				else
				{
					Chat.AddExamineMsgToClient("You try to load the wrong ammo into your weapon.");
					return false;
				}
			}
			else if (ammoType == magazine.ammoType)
			{
				Chat.AddExamineMsgToClient("Your weapon is already loaded, you can't fit more Magazines in it, silly!");
				return false;
			}

			return false;
		}

		[Server]
		public void ServerReloadMagazine(GameObject mag)
		{
			if (CurrentMagazine == null)
			{
				Loggy.LogWarning($"Why is {nameof(CurrentMagazine)} null for {this}?", Category.Firearms);
			}

			if (MagInternal)
			{
				var clip = mag;
				MagazineBehaviour clipComp = clip.GetComponent<MagazineBehaviour>();
				string message = CurrentMagazine.LoadFromClip(clipComp);
				Chat.AddExamineMsgFromServer(ServerHolder, message);
			}
			else
			{
				LoadMagSound();
				var magazine = mag;
				var fromSlot = magazine.GetComponent<Pickupable>().ItemSlot;
				Inventory.ServerTransfer(fromSlot, magSlot);
			}
		}

		[Server]
		public void ServerUnloadMagazine()
		{
			if (MagInternal == true)
			{
				return;
			}
			UnloadMagSound();
			Inventory.ServerDrop(magSlot);
		}

		#endregion

		#region Weapon Sounds

		//This is for smart guns only
		private void OutOfAmmoSfx()
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GunEmptyAlarm, transform.position,
				sourceObj: ServerHolder);
		}

		public void PlayEmptySfx()
		{
			SoundManager.PlayNetworkedAtPos(DryFireSound, transform.position, sourceObj: ServerHolder);
		}

		public void LoadMagSound()
		{
			SoundManager.PlayNetworkedAtPos(loadMagSound, gameObject.AssumedWorldPosServer());
		}

		public void UnloadMagSound()
		{
			SoundManager.PlayNetworkedAtPos(unloadMagSound, transform.position, sourceObj: ServerHolder);
		}

		#endregion

		#region Weapon Network Supporting Methods

		public void SyncIsSuppressed(bool oldValue, bool newValue)
		{
			isSuppressed = newValue;
		}

		public void SyncCameraRecoilConfig(CameraRecoilConfig oldValue, CameraRecoilConfig newValue)
		{
			CameraRecoilConfig = newValue;
		}

		/// <summary>
		/// Applies recoil to calcuate the final direction of the shot
		/// </summary>
		/// <param name="target">normalized target vector</param>
		/// <returns>final position after applying recoil</returns>
		public Vector2 ApplyRecoil(Vector2 target)
		{
			float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
			float angleVariance = MagSyncedRandomFloat(-CurrentRecoilVariance, CurrentRecoilVariance);
			Loggy.LogTraceFormat("angleVariance {0}", Category.Firearms, angleVariance);
			float newAngle = angle * Mathf.Deg2Rad + angleVariance;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		private float MagSyncedRandomFloat(float min, float max)
		{
			return (float) (CurrentMagazine.CurrentRng() * (max - min) + min);
		}

		private void AppendRecoil()
		{
			if (CurrentRecoilVariance < MaxRecoilVariance)
			{
				//get a random recoil
				float randRecoil = MagSyncedRandomFloat(CurrentRecoilVariance, MaxRecoilVariance);
				Loggy.LogTraceFormat("randRecoil {0}", Category.Firearms, randRecoil);
				CurrentRecoilVariance += randRecoil;
				//make sure the recoil is not too high
				if (CurrentRecoilVariance > MaxRecoilVariance)
				{
					CurrentRecoilVariance = MaxRecoilVariance;
				}
			}
		}

		#endregion

		public bool CanSuicide(GameObject performer)
		{
			if (AllowSuicide == false) return false;
			return CurrentMagazine != null && CurrentMagazine.ServerAmmoRemains != 0;
		}

		public IEnumerator OnSuicide(GameObject performer)
		{
			yield return SuicideAction(performer);
		}

		/// <summary>
		/// Because each gun can have it's own functionality related to death (example : russian roulette with a revolver)
		/// this functionality is split in a virtual function that all scripts that inherit from the gun class can modify.
		/// </summary>
		protected virtual IEnumerator SuicideAction(GameObject performer)
		{
			ServerShoot(performer, performer.RegisterTile().LocalPosition.ToLocal(), BodyPartType.Head,
				true);
			var health = performer.GetComponent<LivingHealthMasterBase>();
			health.ApplyDamageAll(performer, health.MaxHealth / 2, AttackType.Bullet, DamageType.Brute);
			yield return null;
		}
	}

	/// <summary>
	/// Generic weapon types
	/// </summary>
	public enum WeaponType
	{
		SemiAutomatic = 0,
		FullyAutomatic = 1,
		Burst = 2
	}
}
