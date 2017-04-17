using NPC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Carbon : Living {
    private int bruteLoss = 0;
    //Oxygen depravation damage (no air in lungs)
    private int oxyLoss = 0;
    //Toxic damage caused by being poisoned or radiated
    private int toxLoss = 0;
    //Burn damage caused by being way too hot, too cold or burnt.
    private int fireLoss = 0;
    //Damage caused by being cloned or ejected from the cloner early. slimes also deal cloneloss damage to victims
    private int cloneLoss = 0;
    //'Retardation' damage caused by someone hitting you in the head with a bible or being infected with brainrot.
    private int brainLoss = 0;
    //Stamina damage, or exhaustion. You recover it slowly naturally, and are stunned if it gets too high. Holodeck and hallucinations deal this.
    private int staminaLoss = 0;
    // Bitwise mask
    private int statusFlags = 0;

    // Not active right now
    // public List<BodyPart> BodyParts = new List<BodyPart>();

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // See carbon.dm update_stat
    public override void UpdateStat()
    {
        if (mobStat != MobConsciousStat.DEAD)
        {
            // TODO Check brain here
            if (health <= MobHealthThreshold.HEALTH_THRESHOLD_DEAD)
                Death();

            if (paralysis || sleeping || oxyLoss > 50 || ((statusFlags & MobStatusFlag.FAKEDEATH) != 0) || health <= MobHealthThreshold.HEALTH_THRESHOLD_CRIT)
            {
                if (mobStat == MobConsciousStat.CONSCIOUS)
                {
                    mobStat = MobConsciousStat.UNCONSCIOUS;
                }
            }
            else
            {
                mobStat = MobConsciousStat.CONSCIOUS;
            }
        }
    }

    private void Death()
    {
        throw new NotImplementedException();
    }
}
