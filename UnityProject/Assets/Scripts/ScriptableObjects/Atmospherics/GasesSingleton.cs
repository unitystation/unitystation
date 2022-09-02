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

		[field: SerializeField]
		public GasSO Plasma { get; private set; }

		[field: SerializeField]
		public GasSO Oxygen { get; private set; }

		[field: SerializeField]
		public GasSO Nitrogen { get; private set; }

		[field: SerializeField]
		public GasSO CarbonDioxide { get; private set; }

		[field: SerializeField]
		public GasSO NitrousOxide { get; private set; }

		[field: SerializeField]
		public GasSO Hydrogen { get; private set; }

		[field: SerializeField]
		public GasSO WaterVapor { get; private set; }

		[field: SerializeField]
		public GasSO BZ { get; private set; }

		[field: SerializeField]
		public GasSO Miasma { get; private set; }

		[field: SerializeField]
		public GasSO Nitryl { get; private set; }

		[field: SerializeField]
		public GasSO Tritium { get; private set; }

		[field: SerializeField]
		public GasSO HyperNoblium { get; private set; }

		[field: SerializeField]
		public GasSO Stimulum { get; private set; }

		[field: SerializeField]
		public GasSO Pluoxium { get; private set; }

		[field: SerializeField]
		public GasSO Freon { get; private set; }

		[field: SerializeField]
		public GasSO Smoke { get; private set; }

		[field: SerializeField]
		public GasSO Ash { get; private set; }

		[field: SerializeField]
		public GasSO CarbonMonoxide { get; private set; }

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
			AddNewGasSo(CarbonMonoxide);
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
