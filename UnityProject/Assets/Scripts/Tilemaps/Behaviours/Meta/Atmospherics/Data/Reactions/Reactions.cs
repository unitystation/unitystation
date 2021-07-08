using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class Reactions
	{
		public const float KOffsetC = 273.15f;
		public const float PlasmaMaintainFire = KOffsetC - 5;
		public const float PlasmaMaxTemperatureGain = KOffsetC + 10000;
		public const float MinimumOxygenContact = 0.009f;
		public const float BurningDelta = 0.5f;
		public const float EnergyPerMole = 30000;


		private static List<Reaction> reactions = new List<Reaction>();

		static Reactions()
		{
			reactions.Add(new PlasmaFireReaction());
		}

		public static void React(GasMix gasMix, Matrix matrix)
		{
			foreach (Reaction reaction in reactions)
			{
				if (reaction.Satisfies(gasMix))
				{
					reaction.React(gasMix, Vector3.zero, matrix);
				}
			}
		}
	}
}