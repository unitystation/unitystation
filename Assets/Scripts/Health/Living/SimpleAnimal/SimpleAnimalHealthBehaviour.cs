
using Objects;
public class SimpleAnimalHealthBehaviour : HealthBehaviour
{
    public override int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType, BodyPartType bodyPartAim)
    {
        base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
            
        //if this living is on the server:
//            if (b != null && b.shooterName != gameObject.name && mobStat != MobConsciousStat.DEAD) {
        var simpleAnimal = GetComponent<SimpleAnimal>();
        if (simpleAnimal != null && 
            simpleAnimal.mobStat != MobConsciousStat.DEAD && 
            CustomNetworkManager.Instance._isServer) 
        {
            //delegated to Living atm
            GetComponent<Living>().ReceiveDamage(damagedBy, damage, damageType, bodyPartAim);
        }
        return damage;

    }

    public override void onDeathActions()
    {
        //handled by Living for now
    }
}