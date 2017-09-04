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
        IsDead = false;
        IsCrit = false;
    }

    public int Health { get; private set; }
    public DamageType LastDamageType { get; private set; }
    public string LastDamagedBy { get; private set; }
    public bool IsCrit { get; private set; }
    public bool IsDead { get; private set; }

    public void ApplyDamage(string damagedBy, int damage,
        DamageType damageType, BodyPartType bodyPartAim = BodyPartType.CHEST)
    {
        if ( damage <= 0 || IsDead ) return;
        var calculatedDamage = ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
        
//        Debug.LogFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}",
//            calculatedDamage, Health, Health - calculatedDamage, gameObject.name, damageType, bodyPartAim, damagedBy);
        Health -= calculatedDamage;
        checkDeadStatus();
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
        OnDeathActions();
        IsDead = true;
    }

    private void checkDeadStatus()
    {
        if ( Health > -100 || IsDead ) return;
        Health = -100;
        Death();
    }

    public void AddHealth(int amount)
    {
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
