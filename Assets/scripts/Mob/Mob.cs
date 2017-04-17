using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Based on mob.dm
public abstract class Mob : NetworkBehaviour {
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    #region mob_defines.dm

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
}
