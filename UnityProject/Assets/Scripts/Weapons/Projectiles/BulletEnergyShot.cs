public class BulletEnergyShot : BulletBehaviour
{
	public override void OnShoot()
	{
		damageType = DamageType.BURN;
	}
}