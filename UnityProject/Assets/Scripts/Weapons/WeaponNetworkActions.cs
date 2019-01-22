using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WeaponNetworkActions : ManagedNetworkBehaviour
{
	private readonly float speed = 7f;
	private bool allowAttack = true;

	//muzzle flash
	private bool isFlashing;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	public bool lerping { get; private set; } //needs to be read by Camera2DFollow

	private float lerpProgress;

	//Lerp parameters
	private Sprite lerpSprite;

	private Vector3 lerpTo;
	public GameObject muzzleFlash;
	private PlayerMove playerMove;
	private PlayerScript playerScript;
	private SoundNetworkActions soundNetworkActions;
	private GameObject spritesObj;

	private GameObject casingPrefab;

	private void Start()
	{
		spritesObj = transform.Find("Sprites").gameObject;
		playerMove = GetComponent<PlayerMove>();
		soundNetworkActions = GetComponent<SoundNetworkActions>();
		playerScript = GetComponent<PlayerScript>();
		lerpSprite = null;

		casingPrefab = Resources.Load("BulletCasing") as GameObject;
	}

	[Command]
	public void CmdLoadMagazine(GameObject weapon, GameObject magazine, string hand)
	{
		Weapon w = weapon.GetComponent<Weapon>();
		NetworkInstanceId networkID = magazine.GetComponent<NetworkIdentity>().netId;
		w.ServerHandleReloadRequest(networkID);
		GetComponent<PlayerNetworkActions>().ClearInventorySlot(hand);
	}

	[Command]
	public void CmdUnloadWeapon(GameObject weapon)
	{
		Weapon w = weapon.GetComponent<Weapon>();

		var cnt = w.CurrentMagazine?.GetComponent<CustomNetTransform>();
		if(cnt != null)
		{
			cnt.InertiaDrop(transform.position, playerMove.speed, playerScript.PlayerSync.ServerState.Impulse);
		} else {
			Logger.Log("Magazine not found for unload weapon", Category.Firearms);
		}

		w.ServerHandleUnloadRequest();
	}

	[Command]
	public void CmdRequestMeleeAttack(GameObject victim, string slot, Vector2 stabDirection,
		BodyPartType damageZone, LayerType layerType)
	{
		if (!playerMove.allowInput ||
			playerMove.isGhost ||
			!victim ||
			!playerScript.playerNetworkActions.SlotNotEmpty(slot) ||
			!playerScript.playerHealth.serverPlayerConscious
		)
		{
			return;
		}
		if (!allowAttack)
		{
			return;
		}

		var weapon = playerScript.playerNetworkActions.Inventory[slot].Item;
		ItemAttributes weaponAttr = weapon.GetComponent<ItemAttributes>();

		// If Tilemap LayerType is not None then it is a tilemap being attacked
		if (layerType != LayerType.None)
		{
			TileChangeManager tileChangeManager = victim.GetComponent<TileChangeManager>();
			MetaTileMap metaTileMap = victim.GetComponent<MetaTileMap>();
			if (tileChangeManager == null)
			{
				return;
			}

			//Tilemap stuff:
			var tileMapDamage = metaTileMap.Layers[layerType].GetComponent<TilemapDamage>();
			if (tileMapDamage != null)
			{
				//Wire cutters should snip the grills instead:
				if (weaponAttr.itemName == "wirecutters" &&
					tileMapDamage.Layer.LayerType == LayerType.Grills)
				{
					tileMapDamage.WireCutGrill((Vector2) transform.position + stabDirection);
					StartCoroutine(AttackCoolDown());
					return;
				}

				tileMapDamage.DoMeleeDamage((Vector2) transform.position + stabDirection,
					gameObject, (int) weaponAttr.hitDamage);

				playerMove.allowInput = false;
				RpcMeleeAttackLerp(stabDirection, weapon);
				StartCoroutine(AttackCoolDown());
				return;
			}
			return;
		}

		//This check cannot be used with TilemapDamage as the transform position is always far away
		if (!playerScript.IsInReach(victim))
		{
			return;
		}

		//Meaty bodies:
		LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();

		if (victimHealth.IsDead && weaponAttr.type == ItemType.Knife)
		{
			if (victim.GetComponent<SimpleAnimal>())
			{
				SimpleAnimal attackTarget = victim.GetComponent<SimpleAnimal>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				playerMove.allowInput = false;
				attackTarget.Harvest();
				soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);
			}
			else
			{
				PlayerHealth attackTarget = victim.GetComponent<PlayerHealth>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				playerMove.allowInput = false;
				attackTarget.Harvest();
				soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);
			}
			return;
		}

		if (victim != gameObject)
		{
			RpcMeleeAttackLerp(stabDirection, weapon);
			playerMove.allowInput = false;
		}

		victimHealth.ApplyDamage(gameObject, (int) weaponAttr.hitDamage, DamageType.Brute, damageZone);
		if (weaponAttr.hitDamage > 0)
		{
			PostToChatMessage.SendItemAttackMessage(weapon, gameObject, victim, (int) weaponAttr.hitDamage, damageZone);
		}

		soundNetworkActions.RpcPlayNetworkSound(weaponAttr.hitSound, transform.position);
		StartCoroutine(AttackCoolDown());

	}

	private IEnumerator AttackCoolDown(float seconds = 0.5f)
	{
		allowAttack = false;
		yield return new WaitForSeconds(seconds);
		allowAttack = true;
	}

	// Harvest should only be used for animals like pete and cows

	[ClientRpc]
	private void RpcMeleeAttackLerp(Vector2 stabDir, GameObject weapon)
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
		lerpFrom = transform.position;
		Vector3 newDir = stabDir * 0.5f;
		newDir.z = lerpFrom.z;
		lerpTo = lerpFrom + newDir;
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
			spritesObj.transform.position = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * speed);
			if (spritesObj.transform.position == lerpTo || lerpProgress > 2f)
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
					lerpFrom = spritesObj.transform.position;
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

	private IEnumerator ShowMuzzleFlash()
	{
		muzzleFlash.gameObject.SetActive(true);
		yield return new WaitForSeconds(0.1f);
		muzzleFlash.gameObject.SetActive(false);
		isFlashing = false;
	}
}