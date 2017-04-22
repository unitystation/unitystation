using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BodyPart : MonoBehaviour
{
    // see body_parts.dm
    public BodyPartType Type;
    public GameObject DropItem;

    public string Zone = "chest"; // TODO This should all be changed to some sort of enum

    public int BruteDamage = 0;
    public int BurnDamage = 0;
    public int MaxDamage = 0;
    public string Description;

    public int brutestate = 0;
    public int burnstate = 0;

    // see body_parts.dm
    public DamageOverlayType? DamageOverlayType; //the type of damage overlay (if any) to use when this bodypart is bruised/burned.
    public Sprite GreenDamageMonitorIcon;
    public Sprite YellowDamageMonitorIcon;
    public Sprite OrangeDamageMonitorIcon;
    public Sprite RedDamageMonitorIcon;
    public Sprite GrayDamageMonitorIcon;


    // See receive_damage in body_parts.dm
    // This is as close as possible to the original definition
    public bool ReceiveDamage<T>(int brute, int burn, bool updatingHealth = true) where T : Living
    {
        //Debug.Log(this.gameObject.name + " received damage brute: " + brute + " burn: " + burn);

        T owner = this.GetComponentInParent<T>();
        if (owner != null && ((owner.StatusFlags & MobStatusFlag.GODMODE) != 0))
            return false; //godmode

        // TODO support damage multipliers
        int[] bruteArr = { brute, 0 };
        brute = bruteArr.Max();

        int[] burnArr = { burn, 0 };
        burn = burnArr.Max();

        int canInflict = MaxDamage - (BruteDamage + BurnDamage);
        if (canInflict <= 0)
            return false;

        if ((brute + burn) < canInflict)
        {
            //Debug.Log(this.gameObject.name + " applying Damage brute: " + brute + " burn: " + burn);
            BruteDamage += brute;
            BurnDamage += burn;
        }
        else
        {
            if (brute > 0)
            {
                if (burn > 0)
                {
                    brute = (int)DMMath.Round((brute / (brute + burn)) * canInflict, 1);
                    burn = canInflict - brute;  //gets whatever damage is left over
                    BruteDamage += brute;
                    BurnDamage += burn;
                }
                else
                {
                    BruteDamage += canInflict;
                }
            }
            else
            {
                if (burn > 0)
                {
                    BurnDamage += canInflict;
                }
                else
                {
                    Debug.Log(this.gameObject.name + " received no damage");
                    return false;
                }
            }
        }

        if (owner != null && updatingHealth)
            owner.UpdateHealth();

        return true;
    }

    //we inform the bodypart of the changes that happened to the owner, or give it the informations from a source mob.
    // see body_parts.dm /obj/item/bodypart/proc/update_limb(dropping_limb, mob/living/carbon/source)
    public void UpdateLimb(Carbon source)
    {
        // need to add animal checks etc here

        if (source is Human)
        {
            Human s = (Human)source;
            DamageOverlayType = s.DamageOverlayType;
        }
    }

    public static bool IsLimb(string zone)
    {
        switch (zone)
        {
            case "chest":
            case "head":
            case "l_arm":
            case "l_leg":
            case "r_arm":
            case "r_leg":
                return true;
            default:
                return false;
        }
    }
}