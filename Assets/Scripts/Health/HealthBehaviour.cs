using UnityEngine;
using UnityEngine.Networking;

public abstract class HealthBehaviour : NetworkBehaviour
{
    public int initialHealth = 100;
    public int maxHealth = 100;

    private void OnEnable()
    {
        if ( initialHealth <= 0 )
        {
            Debug.LogWarningFormat("Initial health ({0}) set to zero/below zero!", initialHealth);
            initialHealth = 1;
        }

        Health = initialHealth;
//        Dead = false;
//        Crit = false;
        ConsciousState = ConsciousState.CONSCIOUS;
    }

    public int Health { get; private set; }
    public DamageType LastDamageType { get; private set; }
    public string LastDamagedBy { get; private set; }
    public ConsciousState ConsciousState;
    public bool IsCrit {
        get { return ConsciousState == ConsciousState.UNCONSCIOUS; }
        private set { ConsciousState = ConsciousState.UNCONSCIOUS; }
    }
    public bool IsDead {
        get { return ConsciousState == ConsciousState.DEAD; }
        private set { ConsciousState = ConsciousState.DEAD; }
    }

    public void ApplyDamage(string damagedBy, int damage,
        DamageType damageType, BodyPartType bodyPartAim = BodyPartType.CHEST)
    {
        if ( damage <= 0 || IsDead ) return;
        var calculatedDamage = ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
        
//        Debug.LogFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}",
//            calculatedDamage, Health, Health - calculatedDamage, gameObject.name, damageType, bodyPartAim, damagedBy);
        Health -= calculatedDamage;
        checkDeadCritStatus();
    }

    public virtual int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType,
        BodyPartType bodyPartAim)
    {
        LastDamageType = damageType;
        LastDamagedBy = damagedBy;
        return damage;
    }

    ///Death from other causes
    protected virtual void Death()
    {
        IsDead = true;
        OnDeathActions();
    }
    protected virtual void Crit()
    {
        IsCrit = true;
        OnCritActions();
    }

    private void checkDeadCritStatus()
    {
        if ( Health < 0 )
        {
           Crit();
        }
        if ( Health > -100 || IsDead ) return;
        Health = -100;
        Death();
    }

    public void AddHealth(int amount)
    {
        if (amount <= 0) return;
        Health += amount;
        
        if ( Health > maxHealth )
        {
            Health = maxHealth;
        }
    }

    public void RestoreHealth()
    {
        Health = initialHealth;
    }

    /// <summary>
    /// placeholder method to make player unconscious upon crit
    /// </summary>
    public virtual void OnCritActions()
    {
        var pna = GetComponent<PlayerNetworkActions>();
        pna.CmdConsciousState(false);
    }

    public abstract void OnDeathActions();
}
