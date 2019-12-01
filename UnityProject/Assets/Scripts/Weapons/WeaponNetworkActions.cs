using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class WeaponNetworkActions : ManagedNetworkBehaviour
{
	private readonly float speed = 7f;
	private bool allowAttack = true;
	float fistDamage = 5;

	//muzzle flash
	private bool isFlashing;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	public bool lerping { get; private set; } //needs to be read by Camera2DFollow

	private float lerpProgress;

	//Lerp parameters
	private Sprite lerpSprite;

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
		lerpSprite = null;
	}

	[Command]
	public void CmdLoadMagazine(GameObject gunObject, GameObject magazine, NamedSlot hand)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		Gun gun = gunObject.GetComponent<Gun>();
		uint networkID = magazine.GetComponent<NetworkIdentity>().netId;
		gun.ServerHandleReloadRequest(networkID);
	}

	[Command]
	public void CmdUnloadWeapon(GameObject gunObject)
	{
		if (!Validations.CanInteract(playerScript, NetworkSide.Server)) return;

		Gun gun = gunObject.GetComponent<Gun>();

		var cnt = gun.CurrentMagazine?.GetComponent<CustomNetTransform>();
		if(cnt != null)
		{
			cnt.InertiaDrop(transform.position, playerScript.PlayerSync.SpeedServer, playerScript.PlayerSync.ServerState.Impulse);
		} else {
			Logger.Log("Magazine not found for unload weapon", Category.Firearms);
		}

		gun.ServerHandleUnloadRequest();
	}

	/// <summary>
	/// Utility function that gets the weapon for you
	/// </summary>
	[Command]
	public void CmdRequestMeleeAttackSlot(GameObject victim, NamedSlot slot, Vector2 stabDirection,
	BodyPartType damageZone, LayerType layerType)
	{
		var weapon = playerScript.ItemStorage.GetNamedItemSlot(slot).ItemObject;
		CmdRequestMeleeAttack(victim, weapon, stabDirection, damageZone, layerType);
	}

	[Command]
	public void CmdRequestMeleeAttack(GameObject victim, GameObject weapon, Vector2 stabDirection,
		BodyPartType damageZone, LayerType layerType)
	{
		var tiles = victim.GetComponent<InteractableTiles>();
		if (tiles)
		{
			//validate based on position of target vector
			if (!Validations.CanApply(playerScript, victim, NetworkSide.Server, targetVector: stabDirection)) return;
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

		if (!allowAttack)
		{
			return;
		}

		ItemAttributesV2 weaponAttr = weapon.GetComponent<ItemAttributesV2>();

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
				tileMapDamage.DoMeleeDamage((Vector2) transform.position + stabDirection,
					gameObject, (int) weaponAttr.ServerHitDamage);

				playerMove.allowInput = false;
				RpcMeleeAttackLerp(stabDirection, weapon);
				StartCoroutine(AttackCoolDown());
				return;
			}
			return;
		}

		//This check cannot be used with TilemapDamage as the transform position is always far away
		if (!playerScript.IsInReach(victim, true))
		{
			return;
		}

		// Consider moving this into a MeleeItemTrigger for knifes
		//Meaty bodies:
		LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();
		if (victimHealth != null && victimHealth.IsDead && weaponAttr.HasTrait(KnifeTrait))
		{
			if (victim.GetComponent<SimpleAnimal>())
			{
				SimpleAnimal attackTarget = victim.GetComponent<SimpleAnimal>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				playerMove.allowInput = false;
				attackTarget.Harvest();
				SoundManager.PlayNetworkedAtPos( "BladeSlice", transform.position );
			}
			else
			{
				PlayerHealth attackTarget = victim.GetComponent<PlayerHealth>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				playerMove.allowInput = false;
				attackTarget.Harvest();
				SoundManager.PlayNetworkedAtPos( "BladeSlice", transform.position );
			}
		}

		if (victim != gameObject)
		{
			RpcMeleeAttackLerp(stabDirection, weapon);
			playerMove.allowInput = false;
		}

		var integrity = victim.GetComponent<Integrity>();
		if (integrity != null)
		{
			//damaging an object
			integrity.ApplyDamage((int)weaponAttr.ServerHitDamage, AttackType.Melee, weaponAttr.ServerDamageType);
		}
		else
		{
			//damaging a living thing
			victimHealth.ApplyDamageToBodypart(gameObject, (int) weaponAttr.ServerHitDamage, AttackType.Melee, weaponAttr.ServerDamageType, damageZone);
		}

		SoundManager.PlayNetworkedAtPos(weaponAttr.ServerHitSound, transform.position);


		if (weaponAttr.ServerHitDamage > 0)
		{
			Chat.AddAttackMsgToChat(gameObject, victim, damageZone, weapon);
		}


		StartCoroutine(AttackCoolDown());
	}

	/// <summary>
	/// Performs a punch attempt from one player to a target.
	/// </summary>
	/// <param name="punchDirection">The direction of the punch towards the victim.</param>
	/// <param name="damageZone">The part of the body that is being punched.</param>
	[Command]
	public void CmdRequestPunchAttack(GameObject victim, Vector2 punchDirection, BodyPartType damageZone)
	{
		var tiles = victim.GetComponent<InteractableTiles>();
		if (tiles)
		{
			//validate based on position of target vector
			if (!Validations.CanApply(playerScript, victim, NetworkSide.Server, targetVector: punchDirection)) return;
		}
		else
		{
			//validate based on position of target object
			if (!Validations.CanApply(playerScript, victim, NetworkSide.Server)) return;
		}

		var victimHealth = victim.GetComponent<LivingHealthBehaviour>();
		var victimRegisterTile = victim.GetComponent<RegisterTile>();
		var rng = new System.Random();

		if (!playerScript.IsInReach(victim, true) || !victimHealth)
		{
			return;
		}

		// If the punch is not self inflicted, do the simple lerp attack animation.
		if (victim != gameObject)
		{
			RpcMeleeAttackLerp(punchDirection, null);
			playerMove.allowInput = false;
		}

		// This is based off the alien/humanoid/attack_hand punch code of TGStation's codebase.
		// Punches have 90% chance to hit, otherwise it is a miss.
		if (90 >= rng.Next(1, 100))
		{
			// The punch hit.
			victimHealth.ApplyDamageToBodypart(gameObject, (int) fistDamage, AttackType.Melee, DamageType.Brute, damageZone);
			if (fistDamage > 0)
			{
				Chat.AddAttackMsgToChat(gameObject, victim, damageZone);
			}

			// Make a random punch hit sound.
			SoundManager.PlayNetworkedAtPos("Punch#", victimRegisterTile.WorldPosition);

			StartCoroutine(AttackCoolDown());
		}
		else
		{
			// The punch missed.
			string victimName = victim.Player()?.Name;
			SoundManager.PlayNetworkedAtPos("PunchMiss", transform.position);
			Chat.AddCombatMsgToChat(gameObject, $"You attempted to punch {victimName} but missed!",
				$"{gameObject.Player()?.Name} has attempted to punch {victimName}!");
		}
	}

	private IEnumerator AttackCoolDown(float seconds = 0.5f)
	{
		allowAttack = false;
		yield return WaitFor.Seconds(seconds);
		allowAttack = true;
	}

	[ClientRpc]
	public void RpcMeleeAttackLerp(Vector2 stabDir, GameObject weapon)
	{
		if (lerping)
		{
			return;
		}

		if (weapon && lerpSprite == null)
		{
			SpriteRenderer spriteRenderer = weapon.GetComponentInChildren<SpriteRenderer>();
			lerpSprite = spriteRenderer.sprite;
		}

		if (lerpSprite != null)
		{
			playerScript.hitIcon.ShowHitIcon(stabDir, lerpSprite);
		}

		Vector3 lerpFromWorld = spritesObj.transform.position;
		Vector3 lerpToWorld = lerpFromWorld + (Vector3)(stabDir * 0.5f);
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
		lerpSprite = null;
	}
}
