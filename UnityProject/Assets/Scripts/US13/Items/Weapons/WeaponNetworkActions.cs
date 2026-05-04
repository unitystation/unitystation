using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Admin.Logs;
using US13.Core.Chat;
using US13.Core.Cooldowns;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;
using US13.Health.Living;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Implants.Limbs;
using US13.Items.Weapons.Melee;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Messages.Server.SoundMessages;
using US13.Player;
using US13.Player.MovementV2;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;
using Random = UnityEngine.Random;

namespace US13.Items.Weapons
{
	public class WeaponNetworkActions : NetworkBehaviour
	{
		[SerializeField]
		private float attackSpeed = 7f;

		[SerializeField]
		private float handDamage = 5;

		[SerializeField]
		private uint chanceToHit = 90;

		[SerializeField]
		private DamageType damageType = DamageType.Brute;

		private bool damageOverwritten = false;

		private float traumaDamageChance = 0;
		private TraumaticDamageTypes tramuticDamageType;

		private bool isForLerpBack;
		private Vector3 lerpFrom;
		public bool lerping { get; private set; } // needs to be read by Camera2DFollow

		private float lerpProgress;

		// Lerp parameters
		private SpriteRenderer spriteRendererSource; // need renderer for shader configuration

		private Vector3 lerpTo;
		private MovementSynchronisation playerMove;
		private PlayerScript playerScript;
		private GameObject spritesObj;

		private void Start()
		{
			spritesObj = transform.Find("Sprites").gameObject;
			playerMove = GetComponent<MovementSynchronisation>();
			playerScript = GetComponent<PlayerScript>();
			spriteRendererSource = null;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		/// <summary>
		/// Perform a melee attack to be performed using the object in the player's active hand. Will be validated and performed if valid.
		/// Also handles punching if weapon is null.
		/// </summary>
		/// <param name="victim"></param>
		/// <param name="attackDirection">vector pointing from attacker to the target</param>
		/// <param name="damageZone">damage zone if attacking mob, otherwise use None</param>
		/// <param name="layerType">layer being attacked if attacking tilemap, otherwise use None</param>
		[Server]
		public void ServerPerformMeleeAttack(GameObject victim, Vector2 attackDirection, BodyPartType damageZone, LayerType layerType)
		{
			if (victim == null) return;
			if (playerMove.ObjectIsBuckling.OrNull()?.gameObject != null && playerMove.ObjectIsBuckling is MovementSynchronisation)
			{
				victim = playerMove.ObjectIsBuckling.gameObject;
			}
			if (Cooldowns.IsOnServer(playerScript, CommonCooldowns.Instance.Melee)) return;
			if (playerMove.AllowInput == false) return;
			if (playerScript.PlayerTypeSettings.CanMelee == false) return;
			if (playerScript.playerHealth.serverPlayerConscious == false) return;

			if (victim.TryGetComponent<InteractableTiles>(out var tiles))
			{
				// validate based on position of target vector
				if (Validations.CanApply(playerScript, victim, NetworkSide.Server, targetVector: attackDirection,
					    apt: Validations.CheckState(x => x.CanMelee)) == false) return;
			}
			else
			{
				// validate based on position of target object
				if (Validations.CanApply(playerScript, victim, NetworkSide.Server,
					    apt: Validations.CheckState(x => x.CanMelee)) == false) return;
			}

			MeleeStats stats = new()
			{
				Damage = handDamage,
				DamageType = damageType,
				WeaponSound = playerScript.PlayerTypeSettings.EmptyMeleeAttackData.PickRandom().hitSound.PickRandom(),
				WeaponVerb = playerScript.PlayerTypeSettings.EmptyMeleeAttackData.PickRandom().attackVerb,
				TraumaDamageChance = traumaDamageChance,
				TraumaticDamageType = tramuticDamageType,
			};

			GameObject weapon = playerScript.PlayerNetworkActions.GetActiveHandItem();
			ItemAttributesV2 weaponAttributes = weapon == null ? null : weapon.GetComponent<ItemAttributesV2>();

			if (weaponAttributes != null)
			{
				stats = MeleeStats.Init(weaponAttributes);
				foreach (var meleeBehaviour in weaponAttributes.CustomMeleeBehaviours)
				{
					stats = ApplyMeleeBehaviour(stats, victim, damageZone, meleeBehaviour);
				}

				AdminLogsManager.AddNewLog(this.gameObject, " Try to attack ", victim, " With  ", weapon, LogCategory.MobDamage);
			}
			else
			{
				//weaponAttributes is null so we are punching
				GameObject activeArm = playerScript.PlayerNetworkActions.activeHand;
				HumanoidArm armStats = activeArm.GetComponent<HumanoidArm>();
				if (armStats != null)
				{
					stats = MeleeStats.Init(armStats);
				}

				AdminLogsManager.AddNewLog(this.gameObject, " Try to attack ", victim, " With Hands ", LogCategory.MobDamage);
			}

			LayerTile attackedTile = null;
			bool didHit = false;

			// If Tilemap LayerType is not None then it is a tilemap being attacked
			if (layerType != LayerType.None)
			{
				var tileChangeManager = victim.GetComponent<TileChangeManager>();
				if (tileChangeManager == null) return; // Make sure its on a matrix that is destructable

				// Tilemap stuff:
				var tileMapDamage = victim.GetComponentInChildren<MetaTileMap>().Layers[layerType].gameObject.GetComponent<TilemapDamage>();
				if (tileMapDamage == null) return;

				var worldPos = (Vector2)transform.position + attackDirection;
				attackedTile = tileChangeManager.InteractableTiles.LayerTileAt(worldPos, true);

				AdminLogsManager.AddNewLog(this.gameObject, $" Attacked Tile at {worldPos.ToString()} ", victim, $" Dealing damage {stats.Damage} ", LogCategory.MobDamage);

				// Tile itself is responsible for playing victim damage sound
				tileMapDamage.ApplyDamage(stats.Damage, AttackType.Melee, worldPos);
				didHit = true;
			}
			// Damaging an object
			else if (victim.TryGetComponent<Integrity>(out var integrity) &&
			         victim.TryGetComponent<Meleeable>(out var meleeable) && meleeable.IsMeleeable)
			{
				if (weaponAttributes != null && weaponAttributes.hitSoundSettings != SoundItemSettings.OnlyItem)
				{
					AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
					SoundManager.PlayNetworkedAtPos(integrity.soundOnHit, gameObject.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
				}

				AdminLogsManager.AddNewLog(this.gameObject, " Attacked ", victim, $" Dealing damage {stats.Damage} Damage to {stats.DamageType} ", LogCategory.MobDamage);
				integrity.ApplyDamage(stats.Damage, AttackType.Melee, stats.DamageType);
				didHit = true;
			}
			// must be a living thing
			else
			{
				// This is based off the alien/humanoid/attack_hand punch code of TGStation's codebase.
				// Punches have 90% chance to hit, otherwise it is a miss.
				if (DMMath.Prob(chanceToHit))
				{
					if (BlockCheck(victim, stats.Damage, stats.DamageType))
					{
						// The attack hit.
						if (victim.TryGetComponent<LivingHealthMasterBase>(out var victimHealth))
						{
							if (weaponAttributes != null)
							{
								foreach (var meleeBehaviour in weaponAttributes.CustomMeleeBehaviours)
								{
									ApplyHitEffects(stats, victim, damageZone, meleeBehaviour);
								}
							}

							AdminLogsManager.AddNewLog(this.gameObject, $" Attacked ", victim, $" Dealing damage {stats.Damage} Of type {stats.DamageType} damageZone {damageZone} traumaDamageChance {stats.TraumaDamageChance} Traumatic damage type {stats.TraumaticDamageType} ", LogCategory.MobDamage);
							victimHealth.ApplyDamageToBodyPart(gameObject, stats.Damage, AttackType.Melee, stats.DamageType, damageZone, traumaDamageChance: stats.TraumaDamageChance, tramuticDamageType: stats.TraumaticDamageType);
							didHit = true;
						}
						//TODO: Remove this when HealthV1 is thrown out
						else if (victim.TryGetComponent<LivingHealthBehaviour>(out var victimHealthOld))
						{
							victimHealthOld.ApplyDamageToBodyPart(gameObject, stats.Damage, AttackType.Melee, stats.DamageType, damageZone);
							didHit = true;
						}
					}
					else
					{
						if (weaponAttributes != null)
						{
							foreach (var meleeBehaviour in weaponAttributes.CustomMeleeBehaviours)
							{
								ApplyBlockEffects(stats, victim, damageZone, meleeBehaviour);
							}
						}
					}
				}
				else
				{
					// The punch missed.
					string victimName = victim.ExpensiveName();
					var miss = playerScript.PlayerTypeSettings.EmptyMeleeAttackData.PickRandom();

					if (miss.missSound.Count > 0)
					{
						SoundManager.PlayNetworkedAtPos(miss.missSound.PickRandom(), transform.position, sourceObj: gameObject);
					}

					if (weaponAttributes != null)
					{
						Chat.AddCombatMsgToChat(gameObject, $"You missed {victimName} with {weapon.ExpensiveName()}!",
							$"{gameObject.ExpensiveName()} missed {victimName} with {weapon.ExpensiveName()}!");
					}
					else
					{
						Chat.AddCombatMsgToChat(gameObject, $"You missed {victimName}!",
							$"{gameObject.ExpensiveName()} missed {victimName}!");
					}
				}
			}

			// common logic to do if we hit something
			if (didHit)
			{
				if (stats.WeaponSound != null)
				{
					SoundManager.PlayNetworkedAtPos(stats.WeaponSound, transform.position, sourceObj: gameObject);
				}

				if (stats.Damage > 0)
				{
					Chat.AddAttackMsgToChat(gameObject, victim, damageZone, weapon,
						attackedTile: attackedTile, customAttackVerb: weaponAttributes == null ? stats.WeaponVerb : null);
				}

				if (victim != gameObject)
				{
					RpcMeleeAttackLerp(attackDirection, weapon);
				}

				stats.HitAction?.Invoke(gameObject, victim);
			}

			Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Melee);
		}


		private MeleeStats ApplyMeleeBehaviour(MeleeStats initialStats, GameObject victim, BodyPartType damageZone, ICustomMeleeBehaviour meleeBehaviour)
		{
			if (meleeBehaviour.IsEnabled == false) return initialStats;

			foreach (var requirement in meleeBehaviour.Requirements)
			{
				if (requirement.CanHit(IHitRequirement.HitRequirementType.Behaviour, gameObject, victim, damageZone, initialStats)) continue;
				return initialStats;
			}
			return meleeBehaviour.CustomMeleeBehaviour(gameObject, victim, damageZone, initialStats);
		}
		private void ApplyHitEffects(MeleeStats initialStats, GameObject victim, BodyPartType damageZone, ICustomMeleeBehaviour meleeBehaviour)
		{
			if (meleeBehaviour.IsEnabled == false) return;

			foreach (var requirement in meleeBehaviour.Requirements)
			{
				if (requirement.CanHit(IHitRequirement.HitRequirementType.OnHit, gameObject, victim, damageZone, initialStats)) continue;
				return;
			}
			meleeBehaviour.OnHitBehaviour(gameObject, victim, damageZone, initialStats);
		}

		private void ApplyBlockEffects(MeleeStats initialStats, GameObject victim, BodyPartType damageZone, ICustomMeleeBehaviour meleeBehaviour)
		{
			if (meleeBehaviour.IsEnabled == false) return;

			foreach (var requirement in meleeBehaviour.Requirements)
			{
				if (requirement.CanHit(IHitRequirement.HitRequirementType.OnBlock, gameObject, victim, damageZone, initialStats)) continue;
				return;
			}
			meleeBehaviour.OnBlockBehaviour(gameObject, victim, damageZone, initialStats);
		}

		[Server]
		private bool BlockCheck(GameObject victim, float damage, DamageType damageType)
		{
			float blockChance = 100f;
			AddressableAudioSource blockSound = null;
			string blockName = null;
			Action<GameObject, float, DamageType> blockAction = null;

			if (victim.TryGetComponent<PlayerScript>(out var victimScript))
			{
				var hand = victimScript.DynamicItemStorage.GetActiveHandSlot();
				if (hand != null)
				{
					var attribs = hand.ItemAttributes;
					if (attribs != null)
					{
						blockChance -= attribs.ServerBlockChance;
						blockSound = attribs.ServerBlockSound;
						blockName = hand.ItemObject.ExpensiveName();
						blockAction = attribs.OnBlock;
					}
				}
			}

			if (DMMath.Prob(blockChance) == false)
			{
				//Victim blocked our attack
				string victimName = victim.ExpensiveName();

				if (blockSound != null)
				{
					SoundManager.PlayNetworkedAtPos(blockSound, transform.position, sourceObj: gameObject);
				}

				Chat.AddCombatMsgToChat(gameObject, $"{victimName} blocks your attack with {blockName}!",
					$"{victimName} blocks {gameObject.ExpensiveName()}'s attack with {blockName}!");

				blockAction?.Invoke(gameObject, damage, damageType);

				return false;
			}
			return true;
		}

		[ClientRpc]
		public void RpcMeleeAttackLerp(Vector2 stabDir, GameObject weapon)
		{
			if (lerping || playerScript == null)
			{
				return;
			}

			if (weapon && spriteRendererSource == null)
			{
				spriteRendererSource = weapon.GetComponentInChildren<SpriteRenderer>();
			}

			if (spriteRendererSource != null)
			{
				var projectile = Spawn.ClientPrefab("hitIcon", playerScript.transform.position, playerScript.transform.parent).GameObject;
				var hitIcon = projectile.GetComponent<HitIcon>();
				hitIcon.ShowHitIcon(stabDir, spriteRendererSource, playerScript);
			}

			Vector3 lerpFromWorld = spritesObj.transform.position;
			Vector3 lerpToWorld = lerpFromWorld + (Vector3)(stabDir * 0.25f);
			Vector3 lerpFromLocal = spritesObj.transform.parent.InverseTransformPoint(lerpFromWorld);
			Vector3 lerpToLocal = spritesObj.transform.parent.InverseTransformPoint(lerpToWorld);
			Vector3 localStabDir = lerpToLocal - lerpFromLocal;

			lerpFrom = lerpFromLocal;
			lerpTo = lerpToLocal;
			lerpProgress = 0f;
			isForLerpBack = true;
			lerping = true;
		}

		// Server lerps
		private void UpdateMe()
		{
			if (lerping)
			{
				lerpProgress += Time.deltaTime;
				spritesObj.transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * attackSpeed);
				if (spritesObj.transform.localPosition == lerpTo || lerpProgress > 2f)
				{
					if (!isForLerpBack)
					{
						ResetLerp();
						spritesObj.transform.localPosition = Vector3.zero;
					}
					else
					{
						// To lerp back from knife attack
						ResetLerp();
						lerpTo = lerpFrom;
						lerpFrom = spritesObj.transform.localPosition;
						lerping = true;
					}
				}
			}
		}

		private void ResetLerp()
		{
			lerpProgress = 0f;
			lerping = false;
			isForLerpBack = false;
			spriteRendererSource = null;
		}

		//MeleeStats should contain all relevant data for handling melee attacks, just to make things a little more clean and sane
		public struct MeleeStats
		{
			public float Damage;
			public DamageType DamageType;
			public AddressableAudioSource WeaponSound;
			public String WeaponVerb;
			public float TraumaDamageChance;
			public TraumaticDamageTypes TraumaticDamageType;
			public Action<GameObject, GameObject> HitAction;
			public string DamageSourceName;

			public static MeleeStats Init(ItemAttributesV2 data)
			{
				return new MeleeStats
				{
					Damage = data.ServerHitDamage,
					DamageType = data.ServerDamageType,
					WeaponSound = data.hitSoundSettings == SoundItemSettings.OnlyObject ? null : data.ServerHitSound,
					WeaponVerb = data.ServerAttackVerbs.PickRandom(),
					TraumaDamageChance = data.TraumaDamageChance,
					TraumaticDamageType = data.TraumaticDamageType,
					HitAction = data.OnMelee,
					DamageSourceName = data.ArticleName,
				};
			}

			public static MeleeStats Init(HumanoidArm data)
			{
				return new MeleeStats
				{
					Damage = data.ArmMeleeDamage,
					DamageType = data.ArmDamageType,
					WeaponSound = data.ArmHitSound.PickRandom(),
					WeaponVerb = data.ArmDamageVerbs.PickRandom(),
					TraumaDamageChance = data.ArmTraumaticChance,
					TraumaticDamageType = data.ArmTraumaticDamage,
					HitAction = null,
				};
			}
		}

		//NOTE: attackverbs and hitsounds for unarmed attacks will be sourced from HumanoidArm instead of this if possible.
		[Serializable]
		public class MeleeData
		{
			public string attackVerb;
			public List<AddressableAudioSource> hitSound = new List<AddressableAudioSource>();
			public List<AddressableAudioSource> missSound = new List<AddressableAudioSource>();
		}
	}
}
