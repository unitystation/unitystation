using UnityEngine;
using UnityEngine.Networking;


public abstract class HealthBehaviour : NetworkBehaviour
{
    public int initialHealth = 100;

    private void OnEnable()
    {
        if ( initialHealth <= 0 )
        {
            Debug.LogWarningFormat("Initial health ({0}) set to zero/below zero!", initialHealth);
            initialHealth = 1;
        }

        Health = initialHealth;
        IsDead = false;
    }

    public int Health { get; private set; }
    public DamageType LastDamageType { get; private set; }
    public string LastDamagedBy { get; private set; }
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


    private void checkDeadStatus()
    {
        if ( Health > 0 || IsDead ) return;
        Health = 0;
        onDeathActions();
        IsDead = true;
    }
    public abstract void onDeathActions();
}
