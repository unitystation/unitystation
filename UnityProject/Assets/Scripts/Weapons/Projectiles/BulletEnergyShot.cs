public class BulletEnergyShot : BulletBehaviour
{
    public override void OnShoot()
    {
        damageType = DamageType.BURN; // sets damage hook to burn damage these are energy weapons
        //Bullet specific stuff
    }
}