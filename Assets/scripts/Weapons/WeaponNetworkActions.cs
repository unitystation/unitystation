﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Weapons;
using PlayGroup;
using Sprites;

public class WeaponNetworkActions: NetworkBehaviour {

    private GameObject spritesObj;
    private PlayerMove playerMove;
    private SoundNetworkActions soundNetworkActions;
    private GameObject bloodSplatPrefab;
	public GameObject muzzleFlash;

    //Lerp parameters
    private float lerpProgress = 0f;
    private bool lerping = false;
    private Vector3 lerpFrom;
    private Vector3 lerpTo;
    private float speed = 7f;
    private bool isForLerpBack = false;

	//muzzle flash
	private bool isFlashing = false;

    void Start() {
        spritesObj = transform.Find("Sprites").gameObject;
        playerMove = GetComponent<PlayerMove>();
        soundNetworkActions = GetComponent<SoundNetworkActions>();
        bloodSplatPrefab = Resources.Load("BloodSplat") as GameObject;
    }

    [Command]
    public void CmdLoadMagazine(GameObject weapon, GameObject magazine) {
		Weapon w = weapon.GetComponent<Weapon>();
		NetworkInstanceId networkID = magazine.GetComponent<NetworkIdentity>().netId;
		w.MagNetID = networkID;
    }

    [Command]
    public void CmdUnloadWeapon(GameObject weapon) {
		Weapon w = weapon.GetComponent<Weapon>();
        NetworkInstanceId networkID = NetworkInstanceId.Invalid;
		w.MagNetID = networkID;
    }

    [Command]
	public void CmdShootBullet(GameObject weapon, GameObject magazine, Vector2 direction, string bulletName) {
		if(!playerMove.allowInput || playerMove.isGhost)
            return;

		//get componants
		Weapon wepBehavior = weapon.GetComponent<Weapon>();
		MagazineBehaviour magBehaviour = magazine.GetComponent<MagazineBehaviour>();

		//reduce ammo for shooting
		magBehaviour.ammoRemains--; //TODO: remove more bullets if burst

		//get the bullet prefab being shot
		GameObject bullet = PoolManager.PoolClientInstantiate(Resources.Load(bulletName) as GameObject, transform.position, Quaternion.identity);
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

		//if we have recoil variance add it, and get the new attack angle
		if (wepBehavior != null && wepBehavior.CurrentRecoilVariance > 0) {
			direction = GetRecoilOffset(wepBehavior, angle);
		}

        BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
        b.Shoot(direction, angle, gameObject.name);

		//add additional recoil after shooting for the next round
		AppendRecoil(wepBehavior, angle);

        //This is used to determine where bullet shot should head towards on client
        Ray2D ray = new Ray2D(transform.position, direction);
		RpcShootBullet(weapon, ray.GetPoint(30f), bulletName);

		//TODO add a check to see if bullet or energy weapon
		SpawnBulletCaseing();
		if (!isFlashing) {
			isFlashing = true;
			StartCoroutine(ShowMuzzleFlash());
		}
    }

    //Bullets are just graphical candy on the client, give them the end point and let 
    //them work out the start pos and direction
    [ClientRpc]
    void RpcShootBullet(GameObject weapon, Vector2 endPos, string bulletName) {
		if(!playerMove.allowInput || playerMove.isGhost)
            return;

		Weapon wepBehavior = weapon.GetComponent<Weapon>();
		if (wepBehavior != null) {
			SoundManager.PlayAtPosition(wepBehavior.FireingSound, transform.position);
		}

        if(CustomNetworkManager.Instance._isServer)
            return;

		GameObject bullet = PoolManager.PoolClientInstantiate(Resources.Load(bulletName) as GameObject, transform.position, Quaternion.identity);
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 dir = (endPos - playerPos).normalized;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
        b.Shoot(dir, angle, gameObject.name);
		if (!isFlashing) {
			isFlashing = true;
			StartCoroutine(ShowMuzzleFlash());
		}
    }

    [Command]
    public void CmdKnifeAttackMob(GameObject npcObj, Vector2 stabDirection) {
		if(!playerMove.allowInput || !allowAttack || playerMove.isGhost)
            return;

        Living attackTarget = npcObj.GetComponent<Living>();
        if(npcObj != gameObject) {
            RpcKnifeAttackLerp(stabDirection);
        }
        attackTarget.RpcReceiveDamage(gameObject.name, 20);
        BloodSplat(npcObj.transform.position, BloodSplatSize.medium);
        soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);

        StartCoroutine(AttackCoolDown());
    }

    private bool allowAttack = true;

    private IEnumerator AttackCoolDown(float seconds = 0.5f) {
        allowAttack = false;
        yield return new WaitForSeconds(seconds);
        allowAttack = true;
    }

    [Command]
    public void CmdKnifeHarvestMob(GameObject npcObj, Vector2 stabDirection) {
		if(!playerMove.allowInput || playerMove.isGhost)
            return;

        Living attackTarget = npcObj.GetComponent<Living>();
        RpcKnifeAttackLerp(stabDirection);
        attackTarget.HarvestIt();
        soundNetworkActions.RpcPlayNetworkSound("BladeSlice", transform.position);
    }

    [ClientRpc]
    void RpcKnifeAttackLerp(Vector2 stabDir) {
        if(lerping)
            return;

        if(PlayerManager.LocalPlayer.name == gameObject.name) {
            PlayerManager.LocalPlayerScript.hitIcon.ShowHitIcon(stabDir);
            PlayerManager.LocalPlayerScript.playerMove.allowInput = false;
        }
        lerpFrom = transform.position;
        Vector3 newDir = stabDir * 0.5f;
        newDir.z = lerpFrom.z;
        lerpTo = lerpFrom + newDir;
        lerpProgress = 0f;
        isForLerpBack = true;
        lerping = true;
    }

    [Server]
    public void BloodSplat(Vector3 pos, BloodSplatSize splatSize) {
        GameObject b = GameObject.Instantiate(bloodSplatPrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(b);
        BloodSplat bSplat = b.GetComponent<BloodSplat>();
        bSplat.SplatBlood(splatSize);
    }

    //Server lerps
    void Update() {
        if(lerping) {
            lerpProgress += Time.deltaTime;
            spritesObj.transform.position = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * speed);
            if(spritesObj.transform.position == lerpTo || lerpProgress > 2f) {
                if(!isForLerpBack) {
                    ResetLerp();
                    spritesObj.transform.localPosition = Vector3.zero;
                    if(PlayerManager.LocalPlayer.name == gameObject.name) {
                        PlayerManager.LocalPlayerScript.playerMove.allowInput = true;
                    }
                } else {
                    //To lerp back from knife attack
                    ResetLerp();
                    lerpTo = lerpFrom;
                    lerpFrom = spritesObj.transform.position;
                    lerping = true;
                }
            }
        }
    }

    void ResetLerp() {
        lerpProgress = 0f;
        lerping = false;
        isForLerpBack = false;
    }

	#region Weapon Network Supporting Methods
	Vector2 GetRecoilOffset(Weapon weapon, float angle) {
		var angleVariance = Random.Range (-weapon.CurrentRecoilVariance, weapon.CurrentRecoilVariance);
		var newAngle = (angle * Mathf.Deg2Rad) + angleVariance;
		Vector2 vec2 = new Vector2 (Mathf.Cos (newAngle), Mathf.Sin (newAngle)).normalized;
		return vec2;
	}

	void AppendRecoil(Weapon weapon, float angle) {
		if (weapon != null && weapon.CurrentRecoilVariance < weapon.MaxRecoilVariance) {
			//get a random recoil
			var randRecoil = Random.Range(weapon.CurrentRecoilVariance, weapon.MaxRecoilVariance);
			weapon.CurrentRecoilVariance += randRecoil;
			//make sure the recoil is not too high
			if (weapon.CurrentRecoilVariance > weapon.MaxRecoilVariance) {
				weapon.CurrentRecoilVariance = weapon.MaxRecoilVariance;
			}
		}
	}

	void SpawnBulletCaseing() {
		GameObject casing = GameObject.Instantiate(Resources.Load("BulletCasing") as GameObject, transform.position, Quaternion.identity);
		NetworkServer.Spawn(casing);
	}
	#endregion

	IEnumerator ShowMuzzleFlash(){
		muzzleFlash.gameObject.SetActive(true);
		yield return new WaitForSeconds(0.1f);
		muzzleFlash.gameObject.SetActive(false);
		isFlashing = false;
	}
}
