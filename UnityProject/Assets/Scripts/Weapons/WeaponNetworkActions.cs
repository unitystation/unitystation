using System.Collections;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;
using Weapons;

public class WeaponNetworkActions : ManagedNetworkBehaviour
{
	private readonly float speed = 7f;
	private bool allowAttack = true;

	//muzzle flash
	private bool isFlashing;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	private bool lerping;

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
		w.MagNetID = networkID;
		GetComponent<PlayerNetworkActions>().ClearInventorySlot(hand);
	}

	[Command]
	public void CmdUnloadWeapon(GameObject weapon)
	{
		Weapon w = weapon.GetComponent<Weapon>();
		NetworkInstanceId networkID = NetworkInstanceId.Invalid;
		w.MagNetID = networkID;
	}

	[Command] 
	public void CmdRequestMeleeAttack(GameObject victim, string slot, Vector2 stabDirection, BodyPartType damageZone)
	{
		if (!playerMove.allowInput 
		    || playerMove.isGhost
		    || !victim
		    || !playerScript.playerNetworkActions.SlotNotEmpty( slot ) 
		    || !PlayerManager.PlayerInReach( victim.transform )
		    ) {
			return;
		}
		var weapon = playerScript.playerNetworkActions.Inventory[slot];
		ItemAttributes weaponAttr = weapon.GetComponent<ItemAttributes>();
		HealthBehaviour victimHealth = victim.GetComponent<HealthBehaviour>();


		// checks object and component existence before defining healthBehaviour variable.
		if (victimHealth.IsDead == false)
		{
			if (!allowAttack)
			{
				return;
			}

			if (victim != gameObject)
			{
				RpcMeleeAttackLerp(stabDirection, weapon);
			}

			victimHealth.ApplyDamage(gameObject, ( int ) weaponAttr.hitDamage, DamageType.BRUTE, damageZone);
			if ( weaponAttr.hitDamage > 0 ) {
				PostToChatMessage.SendItemAttackMessage( weapon, gameObject, victim, (int)weaponAttr.hitDamage, damageZone );
			}

			soundNetworkActions.RpcPlayNetworkSound(weaponAttr.hitSound, transform.position);
			StartCoroutine(AttackCoolDown());

		}
		else
		{
			//Butchering if we can
			if ( weaponAttr.type != ItemType.Knife ) {
				return;
			}
			if (victim.GetComponent<SimpleAnimal>())
			{
				SimpleAnimal attackTarget = victim.GetComponent<SimpleAnimal>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				attackTarget.Harvest();
				soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);
			}
			else
			{
				PlayerHealth attackTarget = victim.GetComponent<PlayerHealth>();
				RpcMeleeAttackLerp(stabDirection, weapon);
				attackTarget.Harvest();
				soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);
			}
		}
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
			if (PlayerManager.LocalPlayer && PlayerManager.LocalPlayer.gameObject.name == gameObject.name)
			{
				PlayerManager.LocalPlayerScript.playerMove.allowInput = true;
			}
		}
		lerpFrom = transform.position;
		Vector3 newDir = stabDir * 0.5f;
		newDir.z = lerpFrom.z;
		lerpTo = lerpFrom + newDir;
		lerpProgress = 0f;
		isForLerpBack = true;
		lerping = true;
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
						if (PlayerManager.LocalPlayer && PlayerManager.LocalPlayer.gameObject.name == gameObject.name)
						{
							PlayerManager.LocalPlayerScript.playerMove.allowInput = true;
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