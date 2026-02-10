using Chemistry;
using US13.ScriptableObjects.Atmospherics;

namespace US13.Systems.Fluids
{
	[System.Serializable]
	public class GasOrReagent
	{
		public Reagent Reagent;
		public GasSO Gas;

		public float Amount;

	}
}
