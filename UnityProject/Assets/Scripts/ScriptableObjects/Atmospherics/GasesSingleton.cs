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

		public GasSO Plasma = null;
		public GasSO Oxygen = null;
		public GasSO Nitrogen = null;
		public GasSO CarbonDioxide = null;
		public GasSO NitrousOxide = null;
		public GasSO Hydrogen = null;
		public GasSO WaterVapor = null;
		public GasSO BZ = null;
		public GasSO Miasma = null;
		public GasSO Nitryl = null;
		public GasSO Tritium = null;
		public GasSO HyperNoblium = null;
		public GasSO Stimulum = null;
		public GasSO Pluoxium = null;
		public GasSO Freon = null;
		public GasSO Smoke = null;
		public GasSO Ash = null;

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
			AddNewGasSo(Plasma);
			AddNewGasSo(Oxygen);
			AddNewGasSo(Nitrogen);
			AddNewGasSo(CarbonDioxide);
			AddNewGasSo(NitrousOxide);
			AddNewGasSo(Hydrogen);
			AddNewGasSo(WaterVapor);
			AddNewGasSo(BZ);
			AddNewGasSo(Miasma);
			AddNewGasSo(Nitryl);
			AddNewGasSo(Tritium);
			AddNewGasSo(HyperNoblium);
			AddNewGasSo(Stimulum);
			AddNewGasSo(Pluoxium);
			AddNewGasSo(Freon);
			AddNewGasSo(Smoke);
			AddNewGasSo(Ash);
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
