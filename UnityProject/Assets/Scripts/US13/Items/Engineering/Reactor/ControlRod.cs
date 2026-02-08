using US13.Objects.Engineering.Reactor;

namespace US13.Items.Engineering.Reactor
{
	public class ControlRod : ReactorChamberRod
	{
		public decimal AbsorptionPower = 4;

		public override RodType GetRodType()
		{
			return RodType.Control;
		}

	}
}
