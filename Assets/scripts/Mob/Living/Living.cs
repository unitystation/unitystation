using NPC;
using PlayGroup;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Sprites;
[Obsolete]
public class Living : Mob
{
//    // Inspector Properties
//    public int InitialMaxHealth = 100;
//
//    private bool shotDmgCoolDown = false;
//
//    //Last person who damaged this living
//	public string lastDamager;
//
//	#region living_defines.dm
//
//    //Maximum health that should be possible.
//    public int maxHealth = 100;
//
//    //A mob's health
//    public int health = 100;
//
//    //Brutal damage caused by brute force (punching, being clubbed by a toolbox ect... this also accounts for pressure damage)
//    public int bruteLoss = 0;
//
//    //Oxygen depravation damage (no air in lungs)
//    public int oxyLoss = 0;
//
//    //Toxic damage caused by being poisoned or radiated
//    public int toxLoss = 0;
//
//    //Burn damage caused by being way too hot, too cold or burnt.
//    public int fireLoss = 0;
//
//    //Damage caused by being cloned or ejected from the cloner early. slimes also deal cloneloss damage to victims
//    public int cloneLoss = 0;
//
//    //'Retardation' damage caused by someone hitting you in the head with a bible or being infected with brainrot.
//    public int brainLoss = 0;
//
//    //Stamina damage, or exhaustion. You recover it slowly naturally, and are stunned if it gets too high. Holodeck and hallucinations deal this.
//    public int staminaLoss = 0;
//
//    // Butcher drops - see var/list/butcher_results = null
//    public List<GameObject> butcherResults = new List<GameObject>();
//
//    #endregion
//
//    // Use this for initialization
//    void Start()
//    {
//        maxHealth = InitialMaxHealth;
//        UpdateHealth();
//    }
//
//    public bool IsClient()
//    {
//        return PlayerManager.LocalPlayer == this.gameObject;
//    }

//    public virtual void OnMouseDown()
//    {
//        if ( UIManager.Hands.CurrentSlot.Item != null && PlayerManager.PlayerInReach( transform ) )
//        {
//            if ( UIManager.Hands.CurrentSlot.Item.GetComponent<ItemAttributes>().type == ItemType.Knife )
//            {
//                Vector2 dir = ( Camera.main.ScreenToWorldPoint( Input.mousePosition ) -
//                                PlayerManager.LocalPlayer.transform.position ).normalized;
//                if ( mobStat != ConsciousState.DEAD )
//                {
//                    PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdKnifeAttackMob( this.gameObject, dir );
//                }
//                else
//                {
//                    if ( !butcherResults.Count.Equals( 0 ) )
//                        PlayerManager.LocalPlayerScript.weaponNetworkActions.CmdKnifeHarvestMob( this.gameObject, dir );
//                }
//            }
//        }
//    }

//	void OnTriggerEnter2D(Collider2D coll){
//		//if this living is on the server:
//		BulletBehaviour b = coll.GetComponent<BulletBehaviour>();
//		if (b != null && b.shooterName != gameObject.name && mobStat != MobConsciousStat.DEAD) {
//		if (CustomNetworkManager.Instance._isServer) {
//				ReceiveBulletDamage(b.shooterName);
//			}
//			PoolManager.PoolClientDestroy(b.gameObject);
//		}
//	}

//
//    [Server] //is it wrong to make it public?
//    public void ReceiveDamage( string damagedBy, int damage, DamageType damageType, BodyPartType bodyPart )
//    {
//        RpcReceiveDamage( damagedBy, damage, damageType, bodyPart );
//
////        BloodSplat( transform.position, BloodSplatSize.medium );
//    }

//    [Server]
//    [Obsolete]
//    void ReceiveBulletDamage( string bulletOwnedBy )
//    {
//        if ( shotDmgCoolDown )
//            return;
//
//        StartCoroutine( ShotDamageCoolDown() );
//        shotDmgCoolDown = true;
//        RpcReceiveDamage( bulletOwnedBy, 20, DamageType.BRUTE, BodyPartType.CHEST );
//        BloodSplat( transform.position, BloodSplatSize.medium );
//    }

//    //FIXME Clean up: this is a duplicate of the same method on WeaponNetworkActions but obviousily NPC's don't have WNA
//    void BloodSplat( Vector3 pos, BloodSplatSize splatSize )
//    {
//		GameObject b = GameObject.Instantiate(Resources.Load( "BloodSplat" ) as GameObject, pos, Quaternion.identity);
//		NetworkServer.Spawn(b);
//		BloodSplat bSplat = b.GetComponent<BloodSplat>();
//		//choose a random blood sprite
//		int spriteNum = 0;
//		switch (splatSize) {
//			case BloodSplatSize.small:
//				spriteNum = UnityEngine.Random.Range(137, 139);
//				break;
//			case BloodSplatSize.medium:
//				spriteNum = UnityEngine.Random.Range(116, 120);
//				break;
//			case BloodSplatSize.large:
//				spriteNum = UnityEngine.Random.Range(105, 108);
//				break;
//		}
//
//		if(spriteNum != 0)
//		bSplat.bloodSprite = spriteNum;
//    }
//
//    IEnumerator ShotDamageCoolDown()
//    {
//        yield return new WaitForSeconds( 0.1f );
//        shotDmgCoolDown = false;
//    }

//    #region unityhelpers
//
//    //If the server is turned into a straight server, this will need to be fixed
//    [ClientRpc]
//    public virtual void RpcReceiveDamage( string damagedBy, int damage,
//        DamageType damageType, BodyPartType bodyPart )
//    {
//        if ( CustomNetworkManager.Instance._isServer )
//        {
//            lastDamager = damagedBy;
//        }
//        ApplyDamage( damage, damageType, bodyPart );
//    }


//    public virtual void HarvestIt()
//    {
//        // This needs to all be moved, see onclick item_attack.dm for harvest() method handling
//        Harvest();
//    }
//
//    #endregion
//
//    #region Living.dm Functions
//
//    // see living.dm /mob/living/proc/update_damage_overlays()
//    public virtual void UpdateDamageOverlays()
//    {
//        return;
//    }
//
//    // see living.dm InCritical
//    public virtual bool InCritical()
//    {
//        return ( health < 0 && health > -95 && mobStat == ConsciousState.UNCONSCIOUS );
//    }
//
//    // see living.dm updateHealth
//    public virtual void UpdateHealth()
//    {
//        if ( ( StatusFlags & MobStatusFlag.GODMODE ) != 0 )
//            return;
//
//        health = maxHealth - oxyLoss - toxLoss - fireLoss - bruteLoss - cloneLoss;
//        UpdateStat();
//    }
//
//    // see living.dm /mob/living/proc/harvest(mob/living/user)
//    [Server]
//    public virtual void Harvest()
//    {
//        foreach ( GameObject harvestPrefab in butcherResults )
//        {
//            GameObject harvest = Instantiate( harvestPrefab, transform.position, Quaternion.identity ) as GameObject;
//            NetworkServer.Spawn( harvest );
//        }
//        RpcGib( false, false, true );
//    }
//
//    #endregion
//
//    #region damage_procs.dm functions
//
//    // see damage_procs.dm /mob/living/proc/apply_damage
//    public virtual int ApplyDamage( int damage, DamageType damagetype, BodyPartType def_zone, int blocked = 0 )
//    {
//        if ( damage <= 0 )
//            return 0;
//
//        switch ( damagetype )
//        {
//            case DamageType.BRUTE:
//                AdjustBruteLoss( damage );
//                break;
//            case DamageType.BURN:
//                AdjustFireLoss( damage );
//                break;
//            default:
//                throw new NotImplementedException();
//        }
//
//        return 1;
//    }

//    // see damage_procs.dm /mob/living/proc/getBruteLoss
//    public virtual int GetBruteLoss()
//    {
//        return bruteLoss;
//    }

//    // see damage_procs.dm /mob/living/proc/adjustBruteLoss
//    public virtual int AdjustBruteLoss( int amount, bool updatingHealth = true, bool forced = false )
//    {
//        if ( !forced && ( ( StatusFlags & MobStatusFlag.GODMODE ) != 0 ) )
//            return 0;
//
//        bruteLoss = DMMath.Clamp( ( bruteLoss + amount ), 0, maxHealth * 2 );
//
//        if ( updatingHealth )
//            UpdateHealth();
//
//        return amount;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getOxyLoss
//    public virtual int GetOxyLoss()
//    {
//        return oxyLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getToxLoss
//    public virtual int GetToxLoss()
//    {
//        return toxLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getFireLoss
//    public virtual int GetFireLoss()
//    {
//        return fireLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/adjustFireLoss
//    public virtual int AdjustFireLoss( int amount, bool updatingHealth = true, bool forced = false )
//    {
//        if ( !forced && ( ( StatusFlags & MobStatusFlag.GODMODE ) != 0 ) )
//            return 0;
//
//        fireLoss = DMMath.Clamp( ( fireLoss + amount ), 0, maxHealth * 2 );
//
//        if ( updatingHealth )
//            UpdateHealth();
//
//        return amount;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getCloneLoss
//    public virtual int GetCloneLoss()
//    {
//        return cloneLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getBrainLoss
//    public virtual int GetBrainLoss()
//    {
//        return brainLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/getStaminaLoss
//    public virtual int GetStaminaLoss()
//    {
//        return staminaLoss;
//    }
//
//    // see damage_procs.dm /mob/living/proc/take_overall_damage
//    public virtual void TakeOverallDamage( int brute, int burn, bool updatingHealth )
//    {
//        AdjustBruteLoss( brute, false ); //zero as argument for no instant health update
//        AdjustFireLoss( burn, false );
//        if ( updatingHealth )
//            UpdateHealth();
//    }
//
//    #endregion

//    #region death.dm
//
//    // see living death.dm /mob/living/death(gibbed)
//    public override void Death( bool gibbed = false )
//    {
//        mobStat = ConsciousState.DEAD;
//        paralysis = false;
//    }

//    // see living death.dm /mob/living/gib(no_brain, no_organs, no_bodyparts)
//    [ClientRpc]
//    public virtual void RpcGib( bool no_brain, bool no_organs, bool no_bodyparts )
//    {
//        if ( mobStat != ConsciousState.DEAD )
//            Death( true );
//
//        GibAnimation();
//
//        gameObject.SetActive( false );
//    }
//
//    // see living death.dm /mob/living/proc/gib_animation()
//    public virtual void GibAnimation()
//    {
//        return;
//    }
//
//    // See living death.dm /mob/living/proc/spawn_gibs()
//    public virtual void SpawnGibs()
//    {
//        GibAnimation();
//    }
//
//    #endregion
//
//    #region helpers.dm
//    [Obsolete]
//    public virtual BodyPart GetBodyPart( string zone )
//    {
//        return null;
//    }
//    public virtual BodyPart GetBodyPart( BodyPartType zone )
//    {
//        return null;
//    }
//
//    #endregion
}