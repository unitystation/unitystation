using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class EntityHealthThreshold
{
    public const int HEALTH_THRESHOLD_CRIT = 0;
    public const int HEALTH_THRESHOLD_DEAD = -100;
}

public partial class EntityConsciousStat
{
    public const int CONSCIOUS = 0;
    public const int UNCONSCIOUS = 1;
    public const int DEAD = 2;
}

public partial class EntityStatusEffect
{
    // See combat.dm
    // Bitflags defining which status effects could be or are inflicted on a mob
    public const int CANSTUN = 1;
    public const int CANWEAKEN = 2;
    public const int CANPARALYSE = 4;
    public const int CANPUSH = 8;
    public const int IGNORESLOWDOWN = 16;
    public const int GOTTAGOFAST = 32;
    public const int GOTTAGOREALLYFAST = 64;
    public const int GODMODE	= 4096;
    public const int FAKEDEATH = 8192;	//Replaces stuff like changeling.changeling_fakedeath
    public const int DISFIGURED = 16384;	//I'll probably move this elsewhere if I ever get wround to writing a bitflag mob-damage system
    public const int XENO_HOST = 32768;	//Tracks whether we're gonna be a baby alien's mummy.
}

public class LivingEntityState : NetworkBehaviour {

    //Maximum health that should be possible.
    public int InitialMaxHealth = 100;

    private int _maxHealth = 100;
    public int MaxHealth
    {
        get
        {
            return _maxHealth;
        }
        [Server]
        set
        {
            _maxHealth = value;
        }
    }


    //A mob's health
    private int _health = 100;
    public int Health
    {
        get
        {
            return _health;
        }
        [Server]
        set
        {
            _health = value;
        }
    }


    //Brutal damage caused by brute force (punching, being clubbed by a toolbox ect... this also accounts for pressure damage)
    private int _bruteLoss = 0;
    public int BruteLoss
    {
        get
        {
            return _bruteLoss;
        }
        [Server]
        set
        {
            _bruteLoss = value;
        }
    }


    //Oxygen depravation damage (no air in lungs)
    private int _oxyLoss = 0;
    public int OxyLoss
    {
        get
        {
            return _oxyLoss;
        }
        [Server]
        set
        {
            _oxyLoss = value;
        }
    }


    //Toxic damage caused by being poisoned or radiated
    private int _toxLoss = 0;
    public int ToxLoss
    {
        get
        {
            return _toxLoss;
        }
        [Server]
        set
        {
            _toxLoss = value;
        }
    }


    //Burn damage caused by being way too hot, too cold or burnt.
    private int _fireLoss = 0;
    public int FireLoss
    {
        get
        {
            return _fireLoss;
        }
        [Server]
        set
        {
            _fireLoss = value;
        }
    }


    //Damage caused by being cloned or ejected from the cloner early. slimes also deal cloneloss damage to victims
    private int _cloneLoss = 0;
    public int CloneLoss
    {
        get
        {
            return _cloneLoss;
        }
        [Server]
        set
        {
            _cloneLoss = value;
        }
    }

    //'Retardation' damage caused by someone hitting you in the head with a bible or being infected with brainrot.
    private int _brainLoss = 0;
    public int BrainLoss
    {
        get
        {
            return _brainLoss;
        }
        [Server]
        set
        {
            _brainLoss = value;
        }
    }

    //Stamina damage, or exhaustion. You recover it slowly naturally, and are stunned if it gets too high. Holodeck and hallucinations deal this.
    private int _staminaLoss = 0;
    public int StaminaLoss
    {
        get
        {
            return _staminaLoss;
        }
        [Server]
        set
        {
            _staminaLoss = value;
        }
    }

    // Bitwise mask
    private int _status_flags = 0;
    public int StatusFlags
    {
        get
        {
            return _status_flags;
        }

        [Server]
        set
        {
            _status_flags = value;
        }
    }

    // Based on living_defines.dm
    private int _stat = EntityConsciousStat.CONSCIOUS;
    public int EntityStat
    {
        get
        {
            return _stat;
        }

        [Server]
        set
        {
            _stat = value;
        }
    }

    // Some statuses based on mob_defines.dm
    private bool _sleeping = false;
    public bool Sleeping
    {
        get
        {
            return _sleeping;
        }

        [Server]
        set
        {
            _sleeping = value;
        }
    }

    // Some statuses based on mob_defines.dm
    private bool _paralysis = false;
    public bool Paralysis
    {
        get
        {
            return _paralysis;
        }
        [Server]
        set
        {
            _paralysis = value;
        }
    }

    // Use this for initialization
    void Start () {
        MaxHealth = InitialMaxHealth;
        UpdateHealth();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    [Server]
    public void UpdateHealth()
    {
        Health = MaxHealth - OxyLoss - ToxLoss - FireLoss - BruteLoss - CloneLoss;
        UpdateStat();
    }

    [Server]
    public void UpdateStat()
    {
        if (EntityStat != EntityConsciousStat.DEAD)
        {
            // TODO Check brain here
            if (Health <= EntityHealthThreshold.HEALTH_THRESHOLD_DEAD)
                Death();

            if (Paralysis || Sleeping || OxyLoss > 50 || ((StatusFlags & EntityStatusEffect.FAKEDEATH) != 0) || Health <= EntityHealthThreshold.HEALTH_THRESHOLD_CRIT)
            {
                if (EntityStat == EntityConsciousStat.CONSCIOUS)
                {
                    EntityStat = EntityConsciousStat.UNCONSCIOUS;
                }
            }
            else
            {
                EntityStat = EntityConsciousStat.CONSCIOUS;
            }
        }
    }


    [Server]
    public void Death()
    {

    }

}
