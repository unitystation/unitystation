using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public static class Reactions
	{
		public const float KOffsetC = 273.15f;
		public const float PlasmaMaintainFire = KOffsetC + 5;
		public const float PlasmaMaxTemperatureGain = KOffsetC + 10000;
		public const float MinimumOxygenContact = 0.009f;
		public const float BurningDelta = 3f;
		public const float EnergyPerMole = 10000;


		private static List<Reaction> reactions = new List<Reaction>();

		static Reactions()
		{
			reactions.Add(new PlasmaFireReaction());
		}

		public static float React(ref GasMix gasMix)
		{
			float consumed = 0;

			foreach (Reaction reaction in reactions)
			{
				if (reaction.Satisfies(gasMix))
				{
					consumed += reaction.React(ref gasMix, Vector3.zero);
				}
			}

			return consumed;
		}
	}
}