using NPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayGroup;

public class Carbon : Living
{
    // Bitwise mask
    private int statusFlags = 0;

    #region carbon_defines.dm
    // see var/list/bodyparts
    public List<GameObject> BodyParts = new List<GameObject>();
    #endregion

    #region carbon.dm
    
    // see carbon.dm update_health_hud()
    public override void UpdateHealthHud(int shownHealthAmount = 0)
    {
        if (!IsClient())
            return;

        if (mobStat != MobConsciousStat.DEAD)
        {
            if (shownHealthAmount == 0)
                shownHealthAmount = health;

            if (shownHealthAmount >= maxHealth)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(0);

            else if (shownHealthAmount > maxHealth * 0.8)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(1);

            else if (shownHealthAmount > maxHealth * 0.6)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(2);

            else if (shownHealthAmount > maxHealth * 0.4)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(3);

            else if (shownHealthAmount > maxHealth * 0.2)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(4);

            else if (shownHealthAmount > 0)
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(5);

            else
                UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(6);
        } else
        {
            UI.UIManager.PlayerHealth.SetShownHealthAmountIcon(7);
        }
    }
    

    // see carbon.dm updatehealth
    public override void UpdateHealth()
    {
        if ((StatusFlags & MobStatusFlag.GODMODE) != 0)
            return;

        int total_burn = 0;
        int total_brute = 0;

        // loop through body parts
        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            total_brute += bodyPart.BruteDamage;
            total_burn += bodyPart.BurnDamage;
        }

        health = maxHealth - GetOxyLoss() - GetToxLoss() - GetCloneLoss() - total_burn - total_brute;
        UpdateStat();

        if (((maxHealth - total_burn) < MobHealthThreshold.HEALTH_THRESHOLD_DEAD) && mobStat == MobConsciousStat.DEAD)
        {
            BecomeHusk();
        }

        base.UpdateHealth();
    }

    // See carbon.dm update_stat
    public override void UpdateStat()
    {
        if (mobStat != MobConsciousStat.DEAD)
        {
            // TODO Check brain here
            if (health <= MobHealthThreshold.HEALTH_THRESHOLD_DEAD)
            {
                Death(false);
                return;
            }

            if (paralysis || sleeping || oxyLoss > 50 || ((statusFlags & MobStatusFlag.FAKEDEATH) != 0) || health <= MobHealthThreshold.HEALTH_THRESHOLD_CRIT)
            {
                if (mobStat == MobConsciousStat.CONSCIOUS)
                {
					Debug.Log("UNCONSCIOUS");
                    mobStat = MobConsciousStat.UNCONSCIOUS;
					if (IsClient()) {
						PlayerManager.LocalPlayerScript.playerNetworkActions.CmdConsciousState(false);
//						//FIXME remove after the combat demo, this is just a fast way to go straight to death

//						mobStat = MobConsciousStat.DEAD;
//						UI.UIManager.PlayerHealth.DisplayDeadScreen();
					}
                }
            } 
			//FIXME: Needs to only become conscious when health improved (atm just hitting an unconcious guy makes him become conscious again
//            else
//            {
//                if (mobStat == MobConsciousStat.UNCONSCIOUS)
//                {
//                    mobStat = MobConsciousStat.CONSCIOUS;
//					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdAllowControlInput(true);
//                }
//
//            }
        }

        UpdateDamageHud();
        UpdateHealthHud();
        // TODO med_hud_set_status();
    }
    
    // 
    public void UpdateDamageHud()
    {
        // Check if player
        if (!IsClient())
            return;

        if (mobStat == MobConsciousStat.UNCONSCIOUS && health <= MobHealthThreshold.HEALTH_THRESHOLD_CRIT)
        {
            int severity = 0;
            if (health >= -20 && health <= -10)
                severity = 1;
            else if (health >= -30 && health <= -20)
                severity = 2;
            else if (health >= -40 && health <= -30)
                severity = 3;
            else if (health >= -50 && health <= -40)
                severity = 4;
            else if (health >= -60 && health <= -50)
                severity = 5;
            else if (health >= -70 && health <= -60)
                severity = 6;
            else if (health >= -80 && health <= -70)
                severity = 7;
            else if (health >= -90 && health <= -80)
                severity = 8;
            else if (health >= -95 && health <= -90)
                severity = 9;
            else if (health < -95)
                severity = 10;

            UI.UIManager.PlayerHealth.DisplayCritScreen(severity);
        } else
        {
            if (oxyLoss > 0)
            {
                int severity = 0;
                if (oxyLoss >= 10 && oxyLoss <= 20)
                    severity = 1;
                else if (oxyLoss >= 10 && oxyLoss <= 20)
                    severity = 2;
                else if (oxyLoss >= 10 && oxyLoss <= 20)
                    severity = 3;
                else if (oxyLoss >= 30 && oxyLoss <= 35)
                    severity = 4;
                else if (oxyLoss >= 35 && oxyLoss <= 40)
                    severity = 5;
                else if (oxyLoss >= 40 && oxyLoss <= 45)
                    severity = 6;
                else if (oxyLoss >= 45)
                    severity = 7;

                UI.UIManager.PlayerHealth.DisplayOxyScreen(severity);
            } else
            {
                UI.UIManager.PlayerHealth.HideOxyScreen();
            }

            //Fire and Brute damage overlay (BSSR)
            int hurtdamage = GetBruteLoss() + GetFireLoss() + damageOverlayTemp;
            if (hurtdamage > 0)
            {
                int severity = 0;
                if (hurtdamage >= 5 && hurtdamage <= 15)
                    severity = 1;
                if (hurtdamage >= 15 && hurtdamage <= 30)
                    severity = 2;
                if (hurtdamage >= 30 && hurtdamage <= 45)
                    severity = 3;
                if (hurtdamage >= 45 && hurtdamage <= 70)
                    severity = 4;
                if (hurtdamage >= 70 && hurtdamage <= 85)
                    severity = 6;
                if (hurtdamage >= 85)
                    severity = 7;

                UI.UIManager.PlayerHealth.DisplayBruteScreen(severity);
            } else
            {
                UI.UIManager.PlayerHealth.HideBruteScreen();
            }
        }
    }
    

    #endregion

    #region update_icons.dm

    // /mob/living/carbon/update_body()
    public virtual void UpdateBody()
    {
        UpdateBodyParts();
    }

    // /mob/living/carbon/proc/update_body_parts()
    public virtual void UpdateBodyParts()
    {
        // TODO update body sprites here
        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            bodyPart.UpdateLimb(this);
        }
    }

    
    // see carbon update_icons /mob/living/carbon/update_damage_overlays()
    public override void UpdateDamageOverlays()
    {
		if (!IsClient())
			return;
        // Kinda similar to the original version
        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            UI.UIManager.PlayerHealth.SetBodyTypeOverlay(bodyPart);
            if (bodyPart.DamageOverlayType != null)
            {

                if (bodyPart.brutestate > 0)
                {
                    UI.UIManager.PlayerHealth.SetBodyPartBruteOverlay(bodyPart);
                    bodyPart.brutestate = 0;
                }

                if (bodyPart.burnstate > 0)
                {
                    UI.UIManager.PlayerHealth.SetBodyPartBurnOverlay(bodyPart);
                    bodyPart.burnstate = 0;
                }
            }

        }
    }

    #endregion

    #region status_procs.dm

    public override int BecomeHusk()
    {
        statusFlags |= MobStatusFlag.DISFIGURED;	//makes them unknown

        UpdateBody();

        return 1;
    }

    #endregion

    #region damage_procs.dm

    public override int ApplyDamage(int damage = 0, string damagetype = DamageType.BRUTE, string def_zone = null, int blocked = 0)
    {
        // TODO Blocking
        if (damage == 0)
            return 0;

        BodyPart BP = null;
        BP = GetBodyPart(def_zone);

        if (BodyPart.IsLimb(def_zone))
        {
            BP = GetBodyPart(def_zone);
        }
        else
        {
            if (def_zone == null)
                def_zone = RandomiseZone(def_zone);

            BP = GetBodyPart(def_zone);

            if (BP == null)
            {
                BP = BodyParts.First().GetComponent<BodyPart>();
            }
        }

        switch (damagetype)
        {
            case DamageType.BRUTE:
                if (BP != null)
                    if (BP.ReceiveDamage<Carbon>(damage, 0) == true)
                    {
                        UpdateDamageOverlays();
                    }  //no bodypart, we deal damage with a more general method.
                    else
                        AdjustBruteLoss(damage);
                break;
            case DamageType.BURN:
                if (BP.ReceiveDamage<Carbon>(0, damage) == true)
                {
                    UpdateDamageOverlays();
                }  //no bodypart, we deal damage with a more general method.
                else
                    AdjustFireLoss(damage);
                break;
            case DamageType.TOX:
                // AdjustToxLoss
                break;
            case DamageType.OXY:
                // AdjustOxyLoss
                break;
            case DamageType.CLONE:
                // AdjustCloneLoss
                break;
            case DamageType.STAMINA:
                // AdjustStaminaLoss
                break;
        }

        return 1;
    }

    //These procs fetch a cumulative total damage from all bodyparts
    ///mob/living/carbon/getBruteLoss()
    public override int GetBruteLoss()
    {
        int amount = 0;

        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            amount += bodyPart.BruteDamage;
        }

        return amount;
    }

    ///mob/living/carbon/getFireLoss()
    public override int GetFireLoss()
    {
        int amount = 0;
        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            amount += bodyPart.BurnDamage;
        }

        return amount;
    }

    ///mob/living/carbon/adjustBruteLoss(amount, updating_health = TRUE, forced = FALSE)
    public override int AdjustBruteLoss(int amount, bool updatingHealth = true, bool forced = false)
    {
        if (!forced && ((StatusFlags & MobStatusFlag.GODMODE) != 0))
            return 0;

        if (amount > 0)
            TakeOverallDamage(amount, 0, updatingHealth);
        // TODO Healing
        //        else
        //            HealOverallDamage(-amount, 0, 0, 1, updatingHealth);

        return amount;
    }

    // /mob/living/carbon/take_overall_damage(brute, burn, updating_health = 1)
    // damage MANY bodyparts, in random order
    public override void TakeOverallDamage(int brute, int burn, bool updatingHealth)
    {
        Debug.Log("Carbon TakeOverallDamage");
        if ((StatusFlags & MobStatusFlag.GODMODE) != 0)
            return; // GODMODE

        bool update = true;

        List<int> indexes = new List<int>();
        for (int i = 0; i < BodyParts.Count; i++)
            indexes.Add(i);

        indexes = indexes.Shuffle<int>().ToList<int>();

        foreach (int i in indexes)
        {
            if (brute < 0 && burn < 0)
                break;

            BodyPart picked = BodyParts[i].GetComponent<BodyPart>();
            int brutePerPart = (int)DMMath.Round(brute / indexes.Count, 0.01);
            int burnPerPart = (int)DMMath.Round(burn / indexes.Count, 0.01);
            int bruteWas = picked.BruteDamage;
            int burnWas = picked.BurnDamage;
            bool tmpUpdate = picked.ReceiveDamage<Carbon>(brutePerPart, burnPerPart, false);
            if (update != true)
                update = tmpUpdate;

            brute -= (picked.BruteDamage - bruteWas);
            burn -= (picked.BurnDamage - burnWas);
        }

        if (updatingHealth)
            UpdateHealth();

        // TODO: Damage overlays
        if (update)
            UpdateDamageOverlays();
    }

    #endregion

    #region helpers.dm
    public override BodyPart GetBodyPart(string zone)
    {
        if (String.IsNullOrEmpty(zone))
            zone = "chest";

        foreach (GameObject bodyPartGameObject in BodyParts)
        {
            BodyPart bodyPart = bodyPartGameObject.GetComponent<BodyPart>();
            if (bodyPart.Zone.Equals(zone))
                return bodyPart;
        }

        return null;
    }
    #endregion

    #region death.dm
    // see carbon/mob/living/carbon/death(gibbed)
    public override void Death(bool gibbed)
    {
		if (!IsClient())
			return;
		
        UI.UIManager.PlayerHealth.DisplayDeadScreen();

        if (mobStat == MobConsciousStat.DEAD)
            return;
        if (!gibbed)
            //TODO emote("deathgasp")

            base.Death(gibbed);
    }
    #endregion

}
