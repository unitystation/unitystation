using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class WeaponNetworkActions : ManagedNetworkBehaviour
{
	private readonly float speed = 7f;
	float fistDamage = 5;

	//muzzle flash
	private bool isFlashing;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	public bool lerping { get; private set; } //needs to be read by Camera2DFollow

	private float lerpProgress;

	//Lerp parameters
	private SpriteRenderer spriteRendererSource; // need renderer for shader configuration

	private Vector3 lerpTo;
	private PlayerMove playerMove;
	private PlayerScript playerScript;
	private GameObject spritesObj;
	public ItemTrait KnifeTrait;

	private void Start()
	{
		spritesObj = transform.Find("Sprites").gameObject;
		playerMove = GetComponent<PlayerMove>();
		playerScript = GetComponent<PlayerScript>();
		spriteRendererSource = null;
	}

	[Command]
	public void CmdLoadMagazine(GameObject gunObject, GameObject magazine, NamedSlot hand)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		Gun gun = gunObject.GetComponent<Gun>();
		uint networkID = magazine.GetComponent<NetworkIdentity>().netId;
		gun.ServerHandleReloadRequest(networkID);
	}

	[Command]
	public void CmdUnloadWeapon(GameObject gunObject)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;
		if (!Cooldowns.TryStartServer(playerScript, CommonCooldowns.Instance.Interaction)) return;

		Gun gun = gunObject.GetComponent<Gun>();

		var cnt = gun.CurrentMagazine?.GetComponent<CustomNetTransform>();
		if (cnt != null)
		{
			cnt.InertiaDrop(transform.position, playerScript.PlayerSync.SpeedServer, playerScript.PlayerSync.ServerState.WorldImpulse);
		} else {
			Logger.Log("Magazine not found for unload weapon", Category.Firearms);
		}

		gun.ServerHandleUnloadRequest();
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
	public void ServerPerformMeleeAttack(GameObject victim, Vector2 attackDirection,
		BodyPartType damageZone, LayerType layerType)
	{
		if (Cooldowns.IsOnServer(playerScript, CommonCooldowns.Instance.Melee)) return;
		var weapon = playerScript.playerNetworkActions.GetActiveHandItem();

		var tiles = victim.GetComponent<InteractableTiles>();
		if (tiles)
		{
			//validate based on position of target vector
			if (!Validations.CanApply(playerScript, victim, NetworkSide.Server, targetVector: attackDirection)) return;
		}
		else
		{
			//validate based on position of target object
			if (!Validations.CanApply(playerScript, victim, NetworkSide.Server)) return;
		}

		if (!playerMove.allowInput ||
			playerScript.IsGhost ||
			!victim ||
			!playerScript.playerHealth.serverPlayerConscious
		)
		{
			return;
		}

		var isWeapon = weapon != null;
		ItemAttributesV2 weaponAttr = isWeapon ? weapon.GetComponent<ItemAttributesV2>() : null;
		var damage = isWeapon ? weaponAttr.ServerHitDamage : fistDamage;
		var damageType = isWeapon ? weaponAttr.ServerDamageType : DamageType.Brute;
		var attackSoundName = isWeapon ? weaponAttr.ServerHitSound : "Punch#";
		LayerTile attackedTile = null;
		bool didHit = false;

		ItemAttributesV2 weaponStats = null;
		if (isWeapon)
		{
			weaponStats = weapon.GetComponent<ItemAttributesV2>();
		}


		// If Tilemap LayerType is not None then it is a tilemap being attacked
		if (layerType != LayerType.None)
		{
			var tileChangeManager = victim.GetComponent<TileChangeManager>();
			if (tileChangeManager == null) return; //Make sure its on a matrix that is destructable

			//Tilemap stuff:
			var tileMapDamage = victim.GetComponentInChildren<MetaTileMap>().Layers[layerType].gameObject
				.GetComponent<TilemapDamage>();
			if (tileMapDamage != null)
			{
				if (isWeapon && weaponStats != null &&
				    weaponStats.hitSoundSettings == SoundItemSettings.OnlyObject)
				{
					attackSoundName = "";
				}
				var worldPos = (Vector2)transform.position + attackDirection;
				attackedTile = tileChangeManager.InteractableTiles.LayerTileAt(worldPos, true);
				tileMapDamage.ApplyDamage((int)damage, AttackType.Melee, worldPos);
				didHit = true;

			}
		}
		else
		{
			//a regular object being attacked

			LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();

			var integrity = victim.GetComponent<Integrity>();
			var meleeable = victim.GetComponent<Meleeable>();
			if (integrity != null)
			{
				if (meleeable.GetMeleeable())
				{//damaging an object
					if(isWeapon && weaponStats != null &&
					weaponStats.hitSoundSettings == SoundItemSettings.Both)
					{
						SoundManager.PlayNetworkedAtPos(integrity.soundOnHit, gameObject.WorldPosServer(), Random.Range(0.9f, 1.1f), sourceObj: gameObject);
					}
					else if (isWeapon && weaponStats != null &&
				    	     weaponStats.hitSoundSettings == SoundItemSettings.OnlyObject && integrity.soundOnHit != "")
					{
						SoundManager.PlayNetworkedAtPos(integrity.soundOnHit, gameObject.WorldPosServer(), Random.Range(0.9f, 1.1f), sourceObj: gameObject);
						attackSoundName = "";
					}
					integrity.ApplyDamage((int)damage, AttackType.Melee, damageType);
					didHit = true;
				}
			}
			else
			{
				//damaging a living thing
				var rng = new System.Random();
				// This is based off the alien/humanoid/attack_hand punch code of TGStation's codebase.
				// Punches have 90% chance to hit, otherwise it is a miss.
				if (isWeapon || 90 >= rng.Next(1, 100))
				{
					if (victimHealth == null || gameObject == null) return;
					// The attack hit.
					victimHealth.ApplyDamageToBodypart(gameObject, (int)damage, AttackType.Melee, damageType, damageZone);
					didHit = true;
				}
				else
				{
					// The punch missed.
					string victimName = victim.Player()?.Name;
					SoundManager.PlayNetworkedAtPos("PunchMiss", transform.position, sourceObj: gameObject);
					Chat.AddCombatMsgToChat(gameObject, $"You attempted to punch {victimName} but missed!",
						$"{gameObject.Player()?.Name} has attempted to punch {victimName}!");
				}
			}
		}

		//common logic to do if we hit something
		if (didHit)
		{
			if (!string.IsNullOrEmpty(attackSoundName))
			{
				SoundManager.PlayNetworkedAtPos(attackSoundName, transform.position, sourceObj: gameObject);
			}

			if (damage > 0)
			{
				Chat.AddAttackMsgToChat(gameObject, victim, damageZone, weapon, attackedTile: attackedTile);
			}
			if (victim != gameObject)
			{
				RpcMeleeAttackLerp(attackDirection, weapon);
				//playerMove.allowInput = false;
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
			playerScript.hitIcon.ShowHitIcon(stabDir, spriteRendererSource);
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

	//Server lerps
	public override void UpdateMe()
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
							CmdRequestInputActivation(); //Ask server if you can move again after melee attack
						}
					}
				}
				else
				{
					//To lerp back from knife attack
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
