using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BodyPart : NetworkBehaviour
{
    // see body_parts.dm
    public BodyPartType Type;
    public GameObject DropItem;

    public int BruteDamage = 0;
    public int BurnDamage = 0;
    public int MaxDamage = 0;
    public string Description;

    public int brutestate = 0;
	public int burnstate = 0;

    // See receive_damage in body_parts.dm
    // This is as close as possible to the original definition
    public int ReceiveDamage(int brute, int burn, bool updatingHealth = true)
    {
        //Debug.Log(this.gameObject.name + " received damage brute: " + brute + " burn: " + burn);

        Living owner = this.GetComponentInParent<Living>();
        if (owner != null && ((owner.StatusFlags & MobStatusFlag.GODMODE) != 0))
            return 0; //godmode

        // TODO support damage multipliers
        int[] bruteArr = { brute, 0 };
        brute = bruteArr.Max();

        int[] burnArr = { burn, 0 };
        burn = burnArr.Max();

        int canInflict = MaxDamage - (BruteDamage + BurnDamage);
        //Debug.Log(this.gameObject.name + " calculated canInflict: " + canInflict + " = " + MaxDamage + " - (" + BruteDamage + " + " + BurnDamage + ")");

        if (canInflict <= 0)
            return 0;

        if ((brute + burn) < canInflict)
        {
            //Debug.Log(this.gameObject.name + " applying Damage brute: " + brute + " burn: " + burn);
            BruteDamage += brute;
            BurnDamage += burn;
            //Debug.Log(this.gameObject.name + " new damage is BruteDamage: " + BruteDamage + " BurnDamage: " + BurnDamage);
        }
        else {
            if (brute > 0)
            {
                if (burn > 0)
                {
                    brute = (int)DMMath.Round((brute / (brute + burn)) * canInflict, 1);
                    burn = canInflict - brute;  //gets whatever damage is left over
                    BruteDamage += brute;
                    BurnDamage += burn;
                } else {
                    BruteDamage += canInflict;
                }
            } else {
                if (burn > 0)
                    BurnDamage += canInflict;
                else
                    return 0;
            }
        }
        
        if (owner != null && updatingHealth)
            owner.UpdateHealth();

        //Debug.Log(this.gameObject.name + " is now BruteDamage: " + BruteDamage + " BurnDamage: " + BurnDamage);

        return UpdateBodyPartDamageState();
    }

    // See bodyparts.dm
    //Updates an organ's brute/burn states for use by update_damage_overlays()
    //Returns 1 if we need to update overlays. 0 otherwise.
    private int UpdateBodyPartDamageState()
    {
        int tbrute = (int)DMMath.Round((BruteDamage / MaxDamage) * 3, 1);
        int tburn = (int)DMMath.Round((BurnDamage / MaxDamage) * 3, 1);

        if ((tbrute != brutestate) || (tburn != burnstate))
        {
            brutestate = tbrute;
            burnstate = tburn;
            return 1;
        }

        return 0;
    }
}