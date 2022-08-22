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

				minimumTileTemperature: AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0f,
				maximumTilePressure: 10000000000,

				//Tritium + Oxygen
				minimumTileMoles: 0.02f,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Plasma + Oxygen
				minimumTileMoles: 0.02f,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: 0,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Freon + Oxygen
				minimumTileMoles: 0.02f,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: AtmosDefines.FUSION_TEMPERATURE_THRESHOLD,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,
				minimumTileMoles: AtmosDefines.FUSION_TRITIUM_MOLES_USED + (2 * AtmosDefines.FUSION_MOLE_THRESHOLD),
				maximumTileMoles: 10000000000,
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
							minimumMolesToReact = 10
						}
					},

					{
						Gas.Nitrogen,
						new GasReactionData()
						{
							minimumMolesToReact = 20
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 5
						}
					}
				},

				minimumTileTemperature: 200,
				maximumTileTemperature: 250,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Oxygen + Nitrogen + BZ
				minimumTileMoles: 35,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: AtmosDefines.N2O_DECOMPOSITION_MIN_ENERGY,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,
				minimumTileMoles: 0.01f,
				maximumTileMoles: 10000000000,
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
							minimumMolesToReact = 10
						}
					},

					{
						Gas.Nitrogen,
						new GasReactionData()
						{
							minimumMolesToReact = 20
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 5
						}
					}
				},

				minimumTileTemperature: 600,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Oxygen + Nitrogen + BZ
				minimumTileMoles: 35,
				maximumTileMoles: 10000000000,
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
							minimumMolesToReact = 10
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = 10
						}
					}
				},

				minimumTileTemperature: 1,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//NO2 + Plasma
				minimumTileMoles: 20,
				maximumTileMoles: 1000000000,
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
							minimumMolesToReact = 40
						}
					},

					{
						Gas.CarbonDioxide,
						new GasReactionData()
						{
							minimumMolesToReact = 20
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 20
						}
					}
				},

				minimumTileTemperature: AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST + 100,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Plasma + CO2 + BZ
				minimumTileMoles: 80,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: 1,
				maximumTileTemperature: AtmosDefines.WATER_VAPOR_FREEZE + 1,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,
				minimumTileMoles: 0.01f,
				maximumTileMoles: 10000000000,
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
							minimumMolesToReact = 30
						}
					},

					{
						Gas.Plasma,
						new GasReactionData()
						{
							minimumMolesToReact = AtmosDefines.MINIMUM_MOLE_COUNT
						}
					},

					{
						Gas.BZ,
						new GasReactionData()
						{
							minimumMolesToReact = 20
						}
					},

					{
						Gas.Nitryl,
						new GasReactionData()
						{
							minimumMolesToReact = 30
						}
					}
				},

				minimumTileTemperature: 1500,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Tritium + Plasma + BZ + Nitryl
				minimumTileMoles: 80,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				// Pluoxium + Stimulum + Nitryl + Plasma
				minimumTileMoles: (AtmosDefines.STIM_BALL_GAS_AMOUNT * 2) + 0.02f,
				maximumTileMoles: 10000000000,
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
							minimumMolesToReact = 10
						}
					},

					{
						Gas.Tritium,
						new GasReactionData()
						{
							minimumMolesToReact = 5
						}
					}
				},

				minimumTileTemperature: AtmosDefines.SPACE_TEMPERATURE,
				maximumTileTemperature: 15,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,

				//Nitrogen + Tritium
				minimumTileMoles: 15,
				maximumTileMoles: 10000000000,
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

				minimumTileTemperature: AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST + 70,
				maximumTileTemperature: 10000000000,
				minimumTilePressure: 0,
				maximumTilePressure: 10000000000,
				minimumTileMoles: 0.01f,
				maximumTileMoles: 10000000000,
				addToBaseReactions: true
			);

			#endregion
		}

		//Gas and minimum moles to react
		public Dictionary<GasSO, GasReactionData> GasReactionData;

		public Reaction Reaction;

		public float MinimumTileTemperature;
		public float MaximumTileTemperature;

		public float MinimumTilePressure;
		public float MaximumTilePressure;

		public float MinimumTileMoles;
		public float MaximumTileMoles;

		public readonly int Index;

		public GasReactions(Dictionary<GasSO, GasReactionData> gasReactionData, Reaction reaction, float minimumTileTemperature, float maximumTileTemperature, float minimumTilePressure, float maximumTilePressure, float minimumTileMoles, float maximumTileMoles, bool addToBaseReactions = false)
		{
			GasReactionData = gasReactionData;

			Reaction = reaction;

			MinimumTileTemperature = minimumTileTemperature;
			MaximumTileTemperature = maximumTileTemperature;
			MinimumTilePressure = minimumTilePressure;
			MaximumTilePressure = maximumTilePressure;
			MinimumTileMoles = minimumTileMoles;
			MaximumTileMoles = maximumTileMoles;

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
