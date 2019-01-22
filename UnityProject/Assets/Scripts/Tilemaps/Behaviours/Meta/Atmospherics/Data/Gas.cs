using System.Collections.Generic;

namespace Atmospherics
{
	public struct Gas
	{
		private static List<Gas> gases = new List<Gas>();

		// Gas constant
		public const float R = 8.3144598f;

		public static readonly Gas Plasma = new Gas("Plasma", 0.8f, 200, 40f);
		public static readonly Gas Oxygen = new Gas("Oxygen", 0.659f, 20, 31.9988f);
		public static readonly Gas Nitrogen = new Gas("Nitrogen", 0.743f, 20, 28.0134f);
		public static readonly Gas CarbonDioxide = new Gas("Carbon Dioxide", 0.655f, 30, 44.01f);

		public readonly float HeatCapacity;
		public readonly float SpecificHeat;
		public readonly float MolarMass;
		public readonly string Name;
		public readonly int Index;

		public static int Count => gases.Count;

		private Gas(string name, float heatCapacity, float specificHeat, float molarMass)
		{
			HeatCapacity = heatCapacity;
			SpecificHeat = specificHeat;
			MolarMass = molarMass;
			Name = name;
			Index = Count;

			gases.Add(this);
		}

		public static Gas Get(int i)
		{
			return gases[i];
		}

		public static IEnumerable<Gas> All => gases.ToArray();

		public static implicit operator int(Gas gas)
		{
			return gas.Index;
		}

	}
}