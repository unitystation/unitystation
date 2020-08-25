using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		float React(ref GasMix gasMix, Vector3 tilePos);
	}

	public struct GasReactions
	{
		private static List<GasReactions> gasReactions = new List<GasReactions>();

		//list of gas reactions:

		#region TritiumFire

		public static readonly GasReactions TritiumFire = new GasReactions(

			reaction: new TritiumFireReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region FreonFire

		public static readonly GasReactions FreonFire = new GasReactions(

			reaction: new FreonFireReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Fusion

		public static readonly GasReactions Fusion = new GasReactions(

			reaction: new FusionReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region NO2

		public static readonly GasReactions NO2Form = new GasReactions(

			reaction: new NO2Formation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:250f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		public static readonly GasReactions NO2Decomp = new GasReactions(

			reaction: new NO2Decomposition(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Nitryl

		public static readonly GasReactions NitrylForm = new GasReactions(

			reaction: new NitrylFormation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region BZ

		public static readonly GasReactions BZForm = new GasReactions(

			reaction: new BZFormation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Freon

		public static readonly GasReactions FreonForm = new GasReactions(

			reaction: new FreonFormation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles:0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region WaterVapour

		public static readonly GasReactions WaterVapour = new GasReactions(

			reaction: new WaterVapourReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles:0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Stimulum

		public static readonly GasReactions StimulumForm = new GasReactions(

			reaction: new StimulumFormation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		public static readonly GasReactions StimBallReaction = new GasReactions(

			reaction: new StimBallReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region HyperNoblium

		public static readonly GasReactions HyperNobliumForm = new GasReactions(

			reaction: new HyperNobliumFormation(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Miasma

		public static readonly GasReactions MiasmaDecomp = new GasReactions(

			reaction: new MiasmaDecomposition(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
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
			maximumTemperature:10000000000f,
			minimumPressure:0f,
			maximumPressure: 10000000000f,
			minimumMoles: 0.01f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		//Gas and minimum moles to react
		public Dictionary<Gas, GasReactionData> GasReactionData;

		public Reaction Reaction;

		public float MinimumTemperature;
		public float MaximumTemperature;

		public float MinimumPressure;
		public float MaximumPressure;

		public float MinimumMoles;
		public float MaximumMoles;

		public float EnergyChange;

		public readonly int Index;

		public GasReactions(Dictionary<Gas, GasReactionData> gasReactionData, Reaction reaction, float minimumTemperature, float maximumTemperature, float minimumPressure, float maximumPressure, float minimumMoles, float maximumMoles, float energyChange)
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

			all = null;
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
	}

	public struct GasReactionData
	{
		public float minimumMolesToReact;

		//unused
		public float ratio;
	}
}