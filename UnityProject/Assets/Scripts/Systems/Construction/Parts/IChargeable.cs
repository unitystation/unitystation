namespace Systems.Construction.Parts
{
	public interface IChargeable
	{

		public bool IsFullyCharged { get;}

		public void ChargeBy(float watts);

	}
}
