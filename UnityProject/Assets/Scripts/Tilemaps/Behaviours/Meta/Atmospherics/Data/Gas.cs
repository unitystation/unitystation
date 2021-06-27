using System;
using System.Collections.Generic;
using System.Linq;
using TileManagement;

namespace Systems.Atmospherics
{
	public struct Gas
	{
		private static Dictionary<GasType, Gas> gases = new  Dictionary<GasType, Gas>(new GasTypeEnum());
		public static Dictionary<GasType, Gas> Gases => gases;

		// Gas constant
		public const float R = 8.3144598f;

		public static readonly Gas Plasma = new Gas(GasType.Plasma,"Plasma", 200, 40f, true, 0.4f, "Plasma", OverlayType.Plasma, 0);
		public static readonly Gas Oxygen = new Gas(GasType.Oxygen,"Oxygen", 20, 31.9988f, false, 0.4f, "NONE", OverlayType.None, 0);
		public static readonly Gas Nitrogen = new Gas(GasType.Nitrogen,"Nitrogen", 20, 28.0134f, false, 0.4f, "NONE", OverlayType.None, 0);
		public static readonly Gas CarbonDioxide = new Gas(GasType.CarbonDioxide,"Carbon Dioxide", 30, 44.01f, false, 0.4f, "NONE", OverlayType.None, 0);

		public static readonly Gas NitrousOxide = new Gas(GasType.NitrousOxide,"Nitrous Oxide", 40, 44.01f, true, 0.4f, "NO2", OverlayType.NO2, 10);
		public static readonly Gas Hydrogen = new Gas(GasType.Hydrogen,"Hydrogen", 30, 44.01f, false, 0.4f, "NONE", OverlayType.None, 0);
		public static readonly Gas WaterVapor = new Gas(GasType.WaterVapor,"Water Vapor", 40, 44.01f, true, 0.4f, "WaterVapour", OverlayType.WaterVapour, 8);
		public static readonly Gas BZ = new Gas(GasType.BZ,"BZ", 20, 44.01f, false, 0.4f, "NONE", OverlayType.None, 8);
		public static readonly Gas Miasma = new Gas(GasType.Miasma,"Miasma", 20, 44.01f, true, 0.4f, "Miasma", OverlayType.Miasma, 0);
		public static readonly Gas Nitryl = new Gas(GasType.Nitryl,"Nitryl", 20, 44.01f, true, 0.4f, "Nitryl", OverlayType.Nitryl, 16);
		public static readonly Gas Tritium = new Gas(GasType.Tritium,"Tritium", 10, 44.01f, true, 0.4f, "Tritium", OverlayType.Tritium, 1);
		public static readonly Gas HyperNoblium = new Gas(GasType.HyperNoblium,"Hyper-Noblium", 2000, 44.01f, false, 0.4f, "NONE", OverlayType.None, 0);
		public static readonly Gas Stimulum = new Gas(GasType.Stimulum,"Stimulum", 5, 44.01f, false, 0.4f, "NONE", OverlayType.None, 7);
		public static readonly Gas Pluoxium = new Gas(GasType.Pluoxium,"Pluoxium", 80, 44.01f, false, 0.4f, "NONE", OverlayType.None, -10);
		public static readonly Gas Freon = new Gas(GasType.Freon,"Freon", 300, 44.01f, true, 0.4f, "Freon", OverlayType.Freon, -5);

		//this is how many Joules are needed to raise 1 mole of the gas 1 degree Kelvin: J/K/mol
		public readonly float MolarHeatCapacity;

		//this is the mass, in grams, of 1 mole of the gas
		public readonly float MolarMass;

		public readonly GasType GasType;
		public readonly string Name;
		public readonly int Index;
		public readonly bool HasOverlay;
		public readonly float MinMolesToSee;
		public readonly string TileName;
		public readonly OverlayType OverlayType;
		public readonly int FusionPower;


		private Gas(GasType gasType, string name, float molarHeatCapacity, float molarMass, bool hasOverlay, float minMolesToSee, string tileName, OverlayType overlayType, int fusionPower)
		{
			GasType = gasType;
			MolarHeatCapacity = molarHeatCapacity;
			MolarMass = molarMass;
			Name = name;
			Index = gases.Count;
			HasOverlay = hasOverlay;
			MinMolesToSee = minMolesToSee;
			TileName = tileName;
			OverlayType = overlayType;
			FusionPower = fusionPower;

			gases.Add(gasType, this);
		}

		public static Gas Get(GasType type)
		{
			return gases[type];
		}

		public static Gas[] All
		{
			get
			{
				if (all == null)
				{
					all = gases.Values.ToArray();
				}

				return all;
			}
		}

		private static Gas[] all;

		public static int Count
		{
			get
			{
				if (numberOfGases == 0)
				{
					numberOfGases = gases.Count;
				}
				return numberOfGases;
			}
		}

		private static int numberOfGases = 0;

		public static implicit operator int(Gas gas)
		{
			return gas.Index;
		}
	}

	[Serializable]
	public class GasData
	{
		//Used for quick iteration
		public GasValues[] GasesArray = new GasValues[0];

		//Used for fast look up for specific gases
		public Dictionary<GasType, GasValues> GasesDict = new Dictionary<GasType, GasValues>( new GasTypeEnum());

		public void RegenerateDict()
		{
			GasesDict.Clear();

			for (int i = 0; i < GasesArray.Length; i++)
			{
				var value = GasesArray[i];
				GasesDict.Add(value.GasType, value);
			}
		}
	}

	[Serializable]
	public class GasValues
	{
		public GasType GasType;

		//Moles of this gas type
		public float Moles;
	}

	public enum GasType
	{
		Oxygen = 0,
		Nitrogen = 1,
		CarbonDioxide = 2,
		Plasma = 3,
		NitrousOxide = 4,
		Hydrogen = 5,
		WaterVapor = 6,
		BZ = 7,
		Miasma = 8,
		Nitryl = 9,
		Tritium = 10,
		HyperNoblium = 11,
		Stimulum = 12,
		Pluoxium = 13,
		Freon = 14
	}

	/// <summary>
	/// Used to avoid boxing for dictionaries that use this enum as its key
	/// </summary>
	public struct GasTypeEnum : IEqualityComparer<GasType>
	{
		public bool Equals(GasType x, GasType y)
		{
			return x == y;
		}

		public int GetHashCode(GasType obj)
		{
			return (int)obj;
		}
	}
}
