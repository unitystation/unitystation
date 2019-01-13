using System.Collections.Generic;

namespace Atmospherics
{
	public static class Reactions
	{
		public const float T0C = 273.15f;
		public const float PLASMA_MINIMUM_BURN_TEMPERATURE = T0C + 100;
		public const float FIRE_MINIMUM_TEMPERATURE_TO_SPREAD = T0C + 150;
		public const float PLASMA_UPPER_TEMPERATURE = T0C + 1370;
		public const float OXYGEN_BURN_RATE_BASE = 1.4f;
		public const float PLASMA_OXYGEN_FULLBURN = 10;
		public const float PLASMA_BURN_RATE_DELTA = 9;
		public const float FIRE_PLASMA_ENERGY_RELEASED = 3000000;

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
					consumed += reaction.React(ref gasMix);
				}
			}

			return consumed;
		}
	}
}