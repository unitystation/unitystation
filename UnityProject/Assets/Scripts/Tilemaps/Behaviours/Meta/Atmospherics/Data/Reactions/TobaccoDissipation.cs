using UnityEngine;

namespace Systems.Atmospherics
{
	public class TobaccoDissipation : Reaction
	{

		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			gasMix.RemoveGas(Gas.Tobacco, gasMix.GetMoles(Gas.Tobacco) * 0.03f);
		}
	}
}
