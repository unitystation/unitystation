using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Logs;
using ScriptableObjects.Atmospherics;
using TileManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Atmospherics
{
	public static class Gas
	{
		//These are here as its easier to put Gas.Gases than GasesSingleton.Instance.Gases
		public static Dictionary<int, GasSO> Gases => GasesSingleton.Instance.Gases;
		public static Dictionary<Reagent, GasSO> ReagentToGas => GasesSingleton.Instance.ReagentToGas;
		public static Dictionary<GasSO, Reagent> GasToReagent => GasesSingleton.Instance.GasToReagent;

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
		public static GasSO Smoke => GasesSingleton.Instance.Smoke;
		public static GasSO Ash => GasesSingleton.Instance.Ash;
		public static GasSO CarbonMonoxide => GasesSingleton.Instance.CarbonMonoxide;
	}

	[Serializable]
	public class GasData
	{
		//Used for quick iteration
		public List<GasValues> GasesArray = new List<GasValues>();

		//Used for fast look up for specific gases
		public Dictionary<int, GasValues> GasesDict = new Dictionary<int, GasValues>();

		public void RegenerateDict()
		{
			lock (GasesArray)
			{
				GasesDict.Clear();
				for (int i = 0; i < GasesArray.Count; i++)
				{
					var value = GasesArray[i];
					GasesDict.Add(value.GasSO, value);
				}
			}
		}

		public void Clear()
		{
			lock (GasesArray)
			{
				for (int i = 0; i < GasesArray.Count; i++)
				{
					GasesArray[i].Pool();
				}
				GasesArray.Clear();
				GasesDict.Clear();
			}
		}
	}

	[Serializable]
	public class GasValues
	{
		public GasSO GasSO;

		[SerializeField, FormerlySerializedAs("Moles")]
		private float moles;

		//Moles of this gas type
		public float Moles
		{
			get => moles;

			set
			{

				if (float.IsNormal(value) == false && value != 0)
				{
					Loggy.LogError($"AAAAAAAAAAAAA REEEEEEEEE Moles Invalid number!!!! {value}");
					return;
				}

				moles = value;

			}
		}

		public void Pool()
		{
			GasSO = null;
			Moles = 0;
			lock (AtmosUtils.PooledGasValues)
			{
				AtmosUtils.PooledGasValues.Add(this);
			}
		}
	}
}