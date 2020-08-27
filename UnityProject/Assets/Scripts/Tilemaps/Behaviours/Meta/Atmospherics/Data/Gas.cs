using System.Collections.Generic;

namespace Atmospherics
{
	public struct Gas
	{
		private static List<Gas> gases = new List<Gas>();

		// Gas constant
		public const float R = 8.3144598f;

		public static readonly Gas Plasma = new Gas("Plasma", 200, 40f, true, 0.4f, "PlasmaAir", -4, 0);
		public static readonly Gas Oxygen = new Gas("Oxygen", 20, 31.9988f, false, 0.4f, "NONE", -3, 0);
		public static readonly Gas Nitrogen = new Gas("Nitrogen", 20, 28.0134f, false, 0.4f, "NONE", -3, 0);
		public static readonly Gas CarbonDioxide = new Gas("Carbon Dioxide", 30, 44.01f, false, 0.4f, "NONE", -3, 0);

		public static readonly Gas NitrousOxide = new Gas("Nitrous Oxide", 40, 44.01f, true, 0.4f, "NO2", -5, 10);
		public static readonly Gas Hydrogen = new Gas("Hydrogen", 30, 44.01f, false, 0.4f, "NONE", -3, 0);
		public static readonly Gas WaterVapor = new Gas("Water Vapor", 40, 44.01f, true, 0.4f, "WaterVapour", -9, 8);
		public static readonly Gas BZ = new Gas("BZ", 20, 44.01f, false, 0.4f, "NONE", -3, 8);
		public static readonly Gas Miasma = new Gas("Miasma", 20, 44.01f, true, 0.4f, "Miasma", -7, 0);
		public static readonly Gas Nitryl = new Gas("Nitryl", 20, 44.01f, true, 0.4f, "Nitryl", -6, 16);
		public static readonly Gas Tritium = new Gas("Tritium", 10, 44.01f, true, 0.4f, "Tritium", -3, 1);
		public static readonly Gas HyperNoblium = new Gas("Hyper-Noblium", 2000, 44.01f, false, 0.4f, "NONE", -3, 0);
		public static readonly Gas Stimulum = new Gas("Stimulum", 5, 44.01f, false, 0.4f, "NONE", -3, 7);
		public static readonly Gas Pluoxium = new Gas("Pluoxium", 80, 44.01f, false, 0.4f, "NONE", -3, -10);
		public static readonly Gas Freon = new Gas("Freon", 300, 44.01f, true, 0.4f, "Freon", -8, -5);

		public readonly float
			MolarHeatCapacity; //this is how many Joules are needed to raise 1 mole of the gas 1 degree Kelvin: J/K/mol

		public readonly float MolarMass; //this is the mass, in grams, of 1 mole of the gas
		public readonly string Name;
		public readonly int Index;
		public readonly bool HasOverlay;
		public readonly float MinMolesToSee;
		public readonly string TileName;
		public readonly int OverlayIndex;
		public readonly int FusionPower;


		private Gas(string name, float molarHeatCapacity, float molarMass, bool hasOverlay, float minMolesToSee, string tileName, int overlayIndex, int fusionPower)
		{
			MolarHeatCapacity = molarHeatCapacity;
			MolarMass = molarMass;
			Name = name;
			Index = gases.Count;
			HasOverlay = hasOverlay;
			MinMolesToSee = minMolesToSee;
			TileName = tileName;
			OverlayIndex = overlayIndex;
			FusionPower = fusionPower;

			gases.Add(this);
		}

		public static Gas Get(int i)
		{
			return gases[i];
		}

		public static Gas[] All
		{
			get
			{
				if (all == null)
				{
					all = gases.ToArray();
				}
				return all;
			}
		}


		private static Gas[] all;


		public static int Count
		{
			get
			{
				if (numberofGases == 0)
				{
					numberofGases = gases.Count;
				}
				return numberofGases;
			}
		}

		private static int numberofGases = 0;

		public static implicit operator int(Gas gas)
		{
			return gas.Index;
		}
	}
}