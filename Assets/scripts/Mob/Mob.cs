using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;

// Based on mob.dm
public abstract class Mob : NetworkBehaviour
{

    #region mob_defines.dm
    // see mob mob_defines.dm 
    public int damageOverlayTemp = 0;
    // see mob_defines.dm /mob/mob_defines.dm Sleeping
    public bool sleeping = false;
    // see mob_defines.dm /mob/mob_defines.dm Paralysis
    public bool paralysis = false;
    // see mob_defines.dm /mob/mob_defines.dm var/stat
    public MobConsciousStat mobStat = MobConsciousStat.CONSCIOUS; // Default to 0 (CONSCIOUS)

    // see mob_defines.dm /mob/mob_defines.dm status_flags
    // Bitwise mask
    public int StatusFlags = MobStatusFlag.CANSTUN | MobStatusFlag.CANWEAKEN | MobStatusFlag.CANPARALYSE | MobStatusFlag.CANPUSH;

    #endregion;

    #region mob.dm

    // see mob.dm /mob/proc/update_stat
    public virtual void UpdateStat()
    {
        return;
    }

    public virtual void UpdateHealthHud(int shownHealthAmount = 0)
    {
        return;
    }

    #endregion

    #region death.dm
    // see mob death.dm /mob/proc/death(gibbed)
    public virtual void Death(bool gibbed)
    {
        return;
    }

    // see mob death.dm /mob/proc/gib()
    //This is the proc for gibbing a mob. Cannot gib ghosts.
    //added different sort of gibs and animations. N
    public virtual void Gib()
    {
        return;
    }
    #endregion

    #region status_procs.dm

    public virtual int BecomeHusk()
    {
        return 0;
    }

    #endregion

    #region mob_helpers.dm

    ///proc/check_zone(zone)
    public string CheckZone(string zone)
    {
        if (zone == null)
            return "chest";

        switch (zone)
        {
            case "eyes":
                return "head";
            case "mouth":
                return "head";
            case "l_hand":
                return "l_arm";
            case "r_hand":
                return "r_arm";
            case "l_foot":
                return "l_leg";
            case "r_foot":
                return "r_leg";
            case "groin":
                return "chest";
            default:
                return "chest";
        }
    }

    ///proc/ran_zone(zone, probability = 80)
    public string RandomiseZone(string zone, int probability = 80)
    {
        if (DMMath.Prob(probability))
            return zone;

        int t = Random.Range(1, 18); // randomly pick a different zone, or maybe the same one

        // sorry!
        switch (t)
        {
            case 1:
                return "head";
            case 2:
                return "chest";
            case 3:
            case 4:
            case 5:
            case 6:
                return "l_arm";
            case 7:
            case 8:
            case 9:
            case 10:
                return "r_arm";
            case 11:
            case 12:
            case 13:
            case 14:
                return "l_leg";
            case 15:
            case 16:
            case 17:
            case 18:
                return "r_leg";
        }

        return zone;
    }

    #endregion

    public void OnMouseEnter()
    {
        UIManager.SetToolTip = this.name;
    }

    public void OnMouseExit()
    {
        UIManager.SetToolTip = "";
    }

    public void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RightClickContextMenu();
        }
    }

    public virtual void RightClickContextMenu()
    {

    }
}