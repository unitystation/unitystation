namespace Objects
{
    public class LivingHealthBehaviour : HealthBehaviour
    {
        public override int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType, BodyPartType bodyPartAim)
        {
            base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

                if (CustomNetworkManager.Instance._isServer) {
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
}