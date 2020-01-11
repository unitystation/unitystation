using System.Collections.Generic;

namespace Atmospherics
{
	public struct Gas
	{
		private static List<Gas> gases = new List<Gas>();

		// Gas constant
		public const float R = 8.3144598f;

		public static readonly Gas Plasma = new Gas("Plasma", 200, 40f);
		public static readonly Gas Oxygen = new Gas("Oxygen", 20, 31.9988f);
		public static readonly Gas Nitrogen = new Gas("Nitrogen", 20, 28.0134f);
		public static readonly Gas CarbonDioxide = new Gas("Carbon Dioxide", 30, 44.01f);

		public readonly float MolarHeatCapacity;	//this is how many Joules are needed to raise 1 mole of the gas 1 degree Kelvin: J/K/mol
		public readonly float MolarMass;	//this is the mass, in grams, of 1 mole of the gas
		public readonly string Name;
		public readonly int Index;

		public static int Count => gases.Count;

		private Gas(string name, float molarHeatCapacity, float molarMass)
		{
			MolarHeatCapacity = molarHeatCapacity;
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