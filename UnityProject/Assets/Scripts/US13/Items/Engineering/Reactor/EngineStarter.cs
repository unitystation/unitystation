using US13.Objects.Engineering.Reactor;

namespace US13.Items.Engineering.Reactor
{
	public class EngineStarter : ReactorChamberRod
	{

		public float NeutronGenerationPerSecond = 4;

		public override RodType GetRodType()
		{
			return RodType.Fuel;
		}
	}
}
