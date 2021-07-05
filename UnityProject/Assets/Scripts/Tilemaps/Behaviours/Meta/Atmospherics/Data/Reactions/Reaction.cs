using System;
using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		void React(GasMix gasMix, MetaDataNode node);
	}

	public struct GasReactions
	{
		private static List<GasReactions> gasReactions = new List<GasReactions>();

		//List of reactions which will be used to reset the gasReactions list so that custom reactions will be removed
		private static List<GasReactions> baseGasReactions = new List<GasReactions>();

		//list of gas reactions:
		private static GasReactions TritiumFire;
		private static GasReactions PlasmaFire;
		private static GasReactions FreonFire;
		private static GasReactions Fusion;
		private static GasReactions NO2Form;
		private static GasReactions NO2Decomp;
		private static GasReactions NitrylForm;
		private static GasReactions BZForm;
		private static GasReactions FreonForm;
		private static GasReactions WaterVapour;
		private static GasReactions StimulumForm;
		private static GasReactions StimBallReaction;
		private static GasReactions HyperNobliumForm;
		private static GasReactions MiasmaDecomp;

		public static void SetUpReactions()
		{
			gasReactions.Clear();

			#region TritiumFire

			TritiumFire = new GasReactions(

				reaction: new TritiumFireReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Tritium,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					},

					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 373.15f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region PlasmaFire

			PlasmaFire = new GasReactions(

				reaction: new PlasmaFireReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					},

					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 373.15f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region FreonFire

			FreonFire = new GasReactions(

				reaction: new FreonFireReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					},

					{
						Gas.Freon,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 1f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region Fusion

			Fusion = new GasReactions(

				reaction: new FusionReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Tritium,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.FUSION_TRITIUM_MOLES_USED
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.FUSION_MOLE_THRESHOLD
						}
					},

					{
						Gas.CarbonDioxide,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.FUSION_MOLE_THRESHOLD
						}
					}
				},

				minimumTemperature: AtmosDefines.FUSION_TEMPERATURE_THRESHOLD,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region NO2

			NO2Form = new GasReactions(

				reaction: new NO2Formation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					},

					{
						Gas.Nitrogen,
						new GasReactionData()
						{
							minimumMolesToReact = 20f
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 5f
						}
					}
				},

				minimumTemperature: 200f,
				maximumTemperature: 250f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			NO2Decomp = new GasReactions(

				reaction: new NO2Decomposition(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.NitrousOxide,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: AtmosDefines.N2O_DECOMPOSITION_MIN_ENERGY,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region Nitryl

			NitrylForm = new GasReactions(

				reaction: new NitrylFormation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Oxygen,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					},

					{
						Gas.Nitrogen,
						new GasReactionData()
						{
							minimumMolesToReact = 20f
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 5f
						}
					}
				},

				minimumTemperature: 600f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region BZ

			BZForm = new GasReactions(

				reaction: new BZFormation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.NitrousOxide,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					}
				},

				minimumTemperature: 1f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region Freon

			FreonForm = new GasReactions(

				reaction: new FreonFormation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 40f
						}
					},

					{
						Gas.CarbonDioxide,
						new GasReactionData()
						{
							minimumMolesToReact = 20f
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 20f
						}
					}
				},

				minimumTemperature: 37315f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region WaterVapour

			WaterVapour = new GasReactions(

				reaction: new WaterVapourReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.WaterVapor,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					},
				},

				minimumTemperature: 1f,
				maximumTemperature: AtmosDefines.WATER_VAPOR_FREEZE + 1,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region Stimulum

			StimulumForm = new GasReactions(

				reaction: new StimulumFormation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Tritium,
						new GasReactionData()
						{
							minimumMolesToReact = 30f
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 20f
						}
					},

					{
						Gas.Nitryl,
						new GasReactionData()
						{
							minimumMolesToReact = 30f
						}
					}
				},

				minimumTemperature: AtmosDefines.STIMULUM_HEAT_SCALE / 2,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			StimBallReaction = new GasReactions(

				reaction: new StimBallReaction(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Pluoxium,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.STIM_BALL_GAS_AMOUNT
						}
					},

					{
						Gas.Stimulum,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.STIM_BALL_GAS_AMOUNT
						}
					},

					{
						Gas.Nitryl,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 373.15f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region HyperNoblium

			HyperNobliumForm = new GasReactions(

				reaction: new HyperNobliumFormation(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Nitrogen,
						new GasReactionData()
						{
							minimumMolesToReact = 10f
						}
					},

					{
						Gas.Tritium,
						new GasReactionData()
						{
							minimumMolesToReact = 5f
						}
					}
				},

				minimumTemperature: 5000000,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion

			#region Miasma

			MiasmaDecomp = new GasReactions(

				reaction: new MiasmaDecomposition(),

				gasReactionData: new Dictionary<GasSO, GasReactionData>()
				{
					{
						Gas.Miasma,
						new GasReactionData()
						{
							minimumMolesToReact = 0.01f
						}
					}
				},

				minimumTemperature: 443.15f,
				maximumTemperature: 10000000000f,
				minimumPressure: 0f,
				maximumPressure: 10000000000f,
				minimumMoles: 0.01f,
				maximumMoles: 10000000000f,
				energyChange: 0f,
				addToBaseReactions: true
			);

			#endregion
		}

		//Gas and minimum moles to react
		public Dictionary<GasSO, GasReactionData> GasReactionData;

		public Reaction Reaction;

		public float MinimumTemperature;
		public float MaximumTemperature;

		public float MinimumPressure;
		public float MaximumPressure;

		public float MinimumMoles;
		public float MaximumMoles;

		public float EnergyChange;

		public readonly int Index;

		public GasReactions(Dictionary<GasSO, GasReactionData> gasReactionData, Reaction reaction, float minimumTemperature, float maximumTemperature, float minimumPressure, float maximumPressure, float minimumMoles, float maximumMoles, float energyChange, bool addToBaseReactions = false)
		{
			GasReactionData = gasReactionData;

			Reaction = reaction;

			MinimumTemperature = minimumTemperature;
			MaximumTemperature = maximumTemperature;
			MinimumPressure = minimumPressure;
			MaximumPressure = maximumPressure;
			MinimumMoles = minimumMoles;
			MaximumMoles = maximumMoles;
			EnergyChange = energyChange;

			Index = gasReactions.Count;

			gasReactions.Add(this);

			if (addToBaseReactions)
			{
				baseGasReactions.Add(this);
			}

			SetAllToNull();
			numberOfGasReactions = 0;
		}

		public static GasReactions Get(int i)
		{
			return gasReactions[i];
		}

		public static GasReactions[] All
		{
			get
			{
				if (all == null)
				{
					all = gasReactions.ToArray();
				}
				return all;
			}
		}


		private static GasReactions[] all;


		public static int Count
		{
			get
			{
				if (numberOfGasReactions == 0)
				{
					numberOfGasReactions = gasReactions.Count;
				}
				return numberOfGasReactions;
			}
		}

		private static int numberOfGasReactions = 0;

		public static implicit operator int(GasReactions gasReaction)
		{
			return gasReaction.Index;
		}

		public static void RemoveReaction(GasReactions gasReaction)
		{
			gasReactions.Remove(gasReaction);
			SetAllToNull();
		}

		/// <summary>
		/// Removes all custom reactions which are added at runtime, only the reactions in this class will stay
		/// </summary>
		public static void ResetReactionList()
		{
			gasReactions = baseGasReactions;
			SetAllToNull();
		}

		private static void SetAllToNull()
		{
			if(all == null) return;

			lock (all)
			{
				all = null;
			}
		}
	}

	public struct GasReactionData
	{
		public float minimumMolesToReact;

		//unused
		public float ratio;
	}
}