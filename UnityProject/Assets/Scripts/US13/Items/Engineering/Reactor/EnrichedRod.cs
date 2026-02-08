using US13.Core.Lifecycle;
using US13.Systems.Explosions;

namespace US13.Items.Engineering.Reactor
{
	public class EnrichedRod : FuelRod
	{
		public override decimal PresentAtoms { get; set; }  = 600000000000000000;
		public override decimal fuelNeutronGeneration { get; set; }  = -0.5M;
		public override decimal PresentAtomsfuel { get; set; }  = 600000000000000000;

		public decimal NeutronSingularity = 76488300000M;

		public override (decimal newEnergy, decimal newNeutrons, bool Break) ProcessRodHit(decimal AbsorbedNeutrons)
		{
			var data = base.ProcessRodHit(AbsorbedNeutrons);

			if (AbsorbedNeutrons > NeutronSingularity)
			{
				Explosion.StartExplosion(CurrentlyInstalledIn.registerObject.WorldPositionServer, 400000);
				data.Break = true;
				if (this != null)
				{
					_ = Despawn.ServerSingle(gameObject);
				}
			}


			return data;
		}
	}
}
