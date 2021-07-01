using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects.Atmospherics;
using TileManagement;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class Gas
	{
		//This is here as its easier to put Gas.Gases than GasesSingleton.Instance.Gases
		public static Dictionary<int, GasSO> Gases => GasesSingleton.Instance.Gases;

		// Gas constant
		public const float R = 8.3144598f;

		public static GasSO Plasma => GasesSingleton.Instance.Plasma;
		public static GasSO Oxygen => GasesSingleton.Instance.Oxygen;
		public static GasSO Nitrogen => GasesSingleton.Instance.Nitrogen;
		public static GasSO CarbonDioxide => GasesSingleton.Instance.CarbonDioxide;

		public static GasSO NitrousOxide => GasesSingleton.Instance.NitrousOxide;
		public static GasSO Hydrogen => GasesSingleton.Instance.Hydrogen;
		public static GasSO WaterVapor => GasesSingleton.Instance.WaterVapor;
		public static GasSO BZ => GasesSingleton.Instance.BZ;
		public static GasSO Miasma => GasesSingleton.Instance.Miasma;
		public static GasSO Nitryl => GasesSingleton.Instance.Nitryl;
		public static GasSO Tritium => GasesSingleton.Instance.Tritium;
		public static GasSO HyperNoblium => GasesSingleton.Instance.HyperNoblium;
		public static GasSO Stimulum => GasesSingleton.Instance.Stimulum;
		public static GasSO Pluoxium => GasesSingleton.Instance.Pluoxium;
		public static GasSO Freon => GasesSingleton.Instance.Freon;
	}

	[Serializable]
	public class GasData
	{
		//Used for quick iteration
		public GasValues[] GasesArray = new GasValues[0];

		//Used for fast look up for specific gases
		public Dictionary<int, GasValues> GasesDict = new Dictionary<int, GasValues>();

		public void RegenerateDict()
		{
			GasesDict.Clear();

			for (int i = 0; i < GasesArray.Length; i++)
			{
				var value = GasesArray[i];
				GasesDict.Add(value.GasSO, value);
			}
		}
	}

	[Serializable]
	public class GasValues
	{
		public GasSO GasSO;

		//Moles of this gas type
		public float Moles;
	}
}
