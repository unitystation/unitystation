using System;
using System.Collections.Generic;
using System.Reflection;
using Systems.Atmospherics;
using Chemistry;
using TileManagement;
using UnityEngine;

namespace ScriptableObjects.Atmospherics
{
	[CreateAssetMenu(fileName = "GasesSingleton", menuName = "Singleton/Atmos/GasesSingleton")]
	public class GasesSingleton : SingletonScriptableObject<GasesSingleton>
	{
		private readonly Dictionary<int, GasSO> gases = new  Dictionary<int, GasSO>();
		public Dictionary<int, GasSO> Gases => gases;

		private readonly Dictionary<Reagent, GasSO> reagentToGas = new  Dictionary<Reagent, GasSO>();
		public Dictionary<Reagent, GasSO> ReagentToGas => reagentToGas;

		private readonly Dictionary<GasSO, Reagent> gasToReagent = new  Dictionary<GasSO, Reagent>();
		public Dictionary<GasSO, Reagent> GasToReagent => gasToReagent;

		[SerializeField]
		private GasSO plasma = null;
		public GasSO Plasma => plasma;

		[SerializeField]
		private GasSO oxygen = null;
		public GasSO Oxygen => oxygen;

		[SerializeField]
		private GasSO nitrogen = null;
		public GasSO Nitrogen => nitrogen;

		[SerializeField]
		private GasSO carbonDioxide = null;
		public GasSO CarbonDioxide => carbonDioxide;

		[SerializeField]
		private GasSO nitrousOxide = null;
		public GasSO NitrousOxide => nitrousOxide;

		[SerializeField]
		private GasSO hydrogen = null;
		public GasSO Hydrogen => hydrogen;

		[SerializeField]
		private GasSO waterVapor = null;
		public GasSO WaterVapor => waterVapor;
	
		[SerializeField]
		private GasSO bz = null;
		public GasSO BZ => bz;
	
		[SerializeField]
		private GasSO miasma = null;
		public GasSO Miasma => miasma;

		[SerializeField]
		private GasSO nitryl = null;
		public GasSO Nitryl => nitryl;

		[SerializeField]
		private GasSO tritium = null;
		public GasSO Tritium => tritium;

		[SerializeField]
		private GasSO hyperNoblium = null;
		public GasSO HyperNoblium => hyperNoblium;

		[SerializeField]
		private GasSO stimulum = null;
		public GasSO Stimulum => stimulum;

		[SerializeField]
		private GasSO pluoxium = null;
		public GasSO Pluoxium => pluoxium;

		[SerializeField]
		private GasSO freon = null;
		public GasSO Freon => freon;

		[SerializeField]
		private GasSO smoke = null;
		public GasSO Smoke => smoke;

		[SerializeField]
		private GasSO ash = null;
		public GasSO Ash => ash;

		[SerializeField]
		private GasSO carbonMonoxide = null;
		public GasSO CarbonMonoxide => carbonMonoxide;

		private void OnEnable()
		{
			SetUpGases();
			GasReactions.SetUpReactions();
		}

		private void SetUpGases()
		{
			gases.Clear();
			reagentToGas.Clear();
			gasToReagent.Clear();

			//Could maybe change this to use reflection?
			AddNewGasSo(plasma);
			AddNewGasSo(oxygen);
			AddNewGasSo(nitrogen);
			AddNewGasSo(carbonDioxide);
			AddNewGasSo(nitrousOxide);
			AddNewGasSo(hydrogen);
			AddNewGasSo(waterVapor);
			AddNewGasSo(bz);
			AddNewGasSo(miasma);
			AddNewGasSo(nitryl);
			AddNewGasSo(tritium);
			AddNewGasSo(hyperNoblium);
			AddNewGasSo(stimulum);
			AddNewGasSo(pluoxium);
			AddNewGasSo(freon);
			AddNewGasSo(smoke);
			AddNewGasSo(ash);
			AddNewGasSo(carbonMonoxide);
		}

		public void AddNewGasSo(GasSO so)
		{
			//Auto make the index based on gases count
			so.SetIndex(gases.Count);
			gases.Add(gases.Count, so);

			if (so.AssociatedReagent == null)
			{
				Debug.LogError($"{so.Name} has null associated reagent");
				return;
			}

			reagentToGas.Add(so.AssociatedReagent, so);
			gasToReagent.Add(so, so.AssociatedReagent);
		}

		/// <summary>
		/// Create a new gas at runtime
		/// </summary>
		public void CreateNewGas(string name, float molarHeatCapacity, float molarMass, bool hasOverlay, float minMolesToSee,
			OverlayTile overlayTile = null, Color? colour = null, int fusionPower = 0, Reagent associatedReagent = null)
		{
			//Create new SO instance
			var newSo = ScriptableObject.CreateInstance<GasSO>();
			newSo.MolarHeatCapacity = molarHeatCapacity;
			newSo.MolarMass = molarMass;
			newSo.Name = name;
			newSo.HasOverlay = hasOverlay;
			newSo.MinMolesToSee = minMolesToSee;
			newSo.OverlayTile = overlayTile;
			newSo.HasOverlay = overlayTile != null;

			if (colour != null)
			{
				newSo.CustomColour = true;
				newSo.Colour = colour.Value;
			}

			newSo.FusionPower = fusionPower;
			newSo.AssociatedReagent = associatedReagent;

			AddNewGasSo(newSo);
		}
	}
}
