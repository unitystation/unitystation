using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TileManagement;
using Mirror;
using AddressableReferences;
using HealthV2;
using Items;
using Messages.Server.SoundMessages;
using Player.Movement;
using Systems.Interaction;

public class WeaponNetworkActions : NetworkBehaviour
{
	[SerializeField]
	private List<AddressableAudioSource> meleeSounds = default;

	private readonly float speed = 7f;
	private readonly float fistDamage = 5;

	private float traumaDamageChance = 0;
	private TraumaticDamageTypes tramuticDamageType;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	public bool lerping { get; private set; } // needs to be read by Camera2DFollow

	private float lerpProgress;

	// Lerp parameters
	private SpriteRenderer spriteRendererSource; // need renderer for shader configuration

	private Vector3 lerpTo;
	private PlayerMove playerMove;
	private PlayerScript playerScript;
	private GameObject spritesObj;

	private void Start()
	{
		spritesObj = transform.Find("Sprites").gameObject;
		playerMove = GetComponent<PlayerMove>();
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
	/// Perform a melee attack to be performed using the object in the player's active hand. Will be validated and performed if valid. Also handles punching
	/// if weapon is null.
	/// </summary>
	/// <param name="victim"></param>
	/// <param name="weapon">null for unarmed attack / punch</param>
	/// <param name="attackDirection">vector pointing from attacker to the target</param>
	/// <param name="damageZone">damage zone if attacking mob, otherwise use None</param>
	/// <param name="layerType">layer being attacked if attacking tilemap, otherwise use None</param>
	[Server]
	public void ServerPerformMeleeAttack(GameObject victim, Vector2 attackDirection, BodyPartType damageZone, LayerType layerType)
	{
		if (victim == null) return;
		if (Cooldowns.IsOnServer(playerScript, CommonCooldowns.Instance.Melee)) return;
		if (playerMove.allowInput == false) return;
		if (playerScript.IsGhost) return;
		if (playerScript.playerHealth.serverPlayerConscious == false) return;

		if (victim.TryGetComponent<InteractableTiles>(out var tiles))
		{
			// validate based on position of target vector
			if (Validations.CanApply(playerScript, victim, NetworkSide.Server, targetVector: attackDirection) == false) return;
		}
		else
		{
			// validate based on position of target object
			if (Validations.CanApply(playerScript, victim, NetworkSide.Server) == false) return;
		}

		float damage = fistDamage;
		DamageType damageType = DamageType.Brute;
		AddressableAudioSource weaponSound = meleeSounds.PickRandom();
		GameObject weapon = playerScript.playerNetworkActions.GetActiveHandItem();
		ItemAttributesV2 weaponAttributes = weapon == null ? null : weapon.GetComponent<ItemAttributesV2>();

		if (weaponAttributes != null)
		{
			damage = weaponAttributes.ServerHitDamage;
			damageType = weaponAttributes.ServerDamageType;
			weaponSound = weaponAttributes.hitSoundSettings == SoundItemSettings.OnlyObject ? null : weaponAttributes.ServerHitSound;
			tramuticDamageType = weaponAttributes.TraumaticDamageType;
			traumaDamageChance = weaponAttributes.TraumaDamageChance;
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

			// Tile itself is responsible for playing victim damage sound
			tileMapDamage.ApplyDamage(damage, AttackType.Melee, worldPos);
			didHit = true;
		}
		// Damaging an object
		else if (victim.TryGetComponent<Integrity>(out var integrity) &&
			victim.TryGetComponent<Meleeable>(out var meleeable) && meleeable.IsMeleeable)
		{
			if (weaponAttributes != null && weaponAttributes.hitSoundSettings != SoundItemSettings.OnlyItem)
			{
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.9f, 1.1f));
				SoundManager.PlayNetworkedAtPos(integrity.soundOnHit, gameObject.WorldPosServer(), audioSourceParameters, sourceObj: gameObject);
			}

			integrity.ApplyDamage(damage, AttackType.Melee, damageType);
			didHit = true;
		}
		// must be a living thing
		else
		{
			// This is based off the alien/humanoid/attack_hand punch code of TGStation's codebase.
			// Punches have 90% chance to hit, otherwise it is a miss.
			if (DMMath.Prob(90))
			{
				// The attack hit.
				if (victim.TryGetComponent<LivingHealthMasterBase>(out var victimHealth))
				{
					victimHealth.ApplyDamageToBodyPart(gameObject, damage, AttackType.Melee, damageType, damageZone, traumaDamageChance: traumaDamageChance, tramuticDamageType: tramuticDamageType);
					didHit = true;
				}
				else if (victim.TryGetComponent<LivingHealthBehaviour>(out var victimHealthOld))
				{
					victimHealthOld.ApplyDamageToBodyPart(gameObject, damage, AttackType.Melee, damageType, damageZone);
					didHit = true;
				}
			}
			else
			{
				// The punch missed.
				string victimName = victim.ExpensiveName();
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.PunchMiss, transform.position, sourceObj: gameObject);
				Chat.AddCombatMsgToChat(gameObject, $"You attempted to punch {victimName} but missed!",
					$"{gameObject.ExpensiveName()} has attempted to punch {victimName}!");
			}
		}

		// common logic to do if we hit something
		if (didHit)
		{
			if (weaponSound != null)
			{
				SoundManager.PlayNetworkedAtPos(weaponSound, transform.position, sourceObj: gameObject);
			}

			if (damage > 0)
			{
				Chat.AddAttackMsgToChat(gameObject, victim, damageZone, weapon, attackedTile: attackedTile);
			}
			if (victim != gameObject)
			{
				RpcMeleeAttackLerp(attackDirection, weapon);
			}
		}

		Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Melee);
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

	[Command]
	private void CmdRequestInputActivation()
	{
		if (playerScript.playerHealth.serverPlayerConscious)
		{
			playerMove.allowInput = true;
		}
		else
		{
			playerMove.allowInput = false;
		}
	}

	// Server lerps
	private void UpdateMe()
	{
		if (lerping)
		{
			lerpProgress += Time.deltaTime;
			spritesObj.transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * speed);
			if (spritesObj.transform.localPosition == lerpTo || lerpProgress > 2f)
			{
				if (!isForLerpBack)
				{
					ResetLerp();
					spritesObj.transform.localPosition = Vector3.zero;
					if (PlayerManager.LocalPlayer)
					{
						if (PlayerManager.LocalPlayer == gameObject)
						{
							CmdRequestInputActivation(); // Ask server if you can move again after melee attack
						}
					}
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
}
