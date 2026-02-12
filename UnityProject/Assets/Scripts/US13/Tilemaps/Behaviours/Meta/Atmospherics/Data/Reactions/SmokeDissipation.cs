namespace US13.Tilemaps.Behaviours.Meta.Atmospherics.Data.Reactions
{
	public class SmokeDissipation : Reaction
	{

		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			gasMix.RemoveGas(Gas.Smoke, gasMix.GetMoles(Gas.Smoke) * 0.03f);

		}
	}
}
