using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine.Networking;
using UnityEngine.XR.WSA.Persistence;

namespace Atmospherics
{
	public struct Gas
	{
		private static List<Gas> gases = new List<Gas>();

		// Gas constant
		public const float R = 8.3144598f;

		public static readonly Gas Plasma = new Gas("Plasma", 0.8f, 40f);
		public static readonly Gas Oxygen = new Gas("Oxygen", 0.659f, 31.9988f);
		public static readonly Gas Nitrogen = new Gas("Nitrogen", 0.743f, 28.0134f);
		public static readonly Gas CarbonDioxide = new Gas("Carbon Dioxide", 0.655f, 44.01f);

		public readonly float HeatCapacity;
		public readonly float MolarMass;
		public readonly string Name;
		public readonly int Index;

		public static int Count => gases.Count;

		private Gas(string name, float heatCapacity, float molarMass)
		{
			HeatCapacity = heatCapacity;
			MolarMass = molarMass;
			Name = name;
			Index = Count;

			gases.Add(this);
		}

		public static Gas Get(int i)
		{
			return gases[i];
		}
	}
}