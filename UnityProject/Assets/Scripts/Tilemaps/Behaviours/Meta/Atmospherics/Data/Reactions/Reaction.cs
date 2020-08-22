using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		float React(ref GasMix gasMix);
	}

	public struct GasReactions
	{
		private static List<GasReactions> gasReactions = new List<GasReactions>();

		//list of gas reactions:

		#region NO2

		public static readonly GasReactions NO2Form = new GasReactions(

			gasCreated: Gas.NitrousOxide,

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
			maximumPressure: 100000000f,
			minimumMoles: 0.1f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Nitryl

		public static readonly GasReactions NitrylForm = new GasReactions(

			gasCreated: Gas.Nitryl,

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
			maximumTemperature:100000000f,
			minimumPressure:0f,
			maximumPressure: 100000000f,
			minimumMoles: 0.1f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region BZ

		public static readonly GasReactions BZForm = new GasReactions(

			gasCreated: Gas.BZ,

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
			maximumTemperature:10000000f,
			minimumPressure:0f,
			maximumPressure: 100000000f,
			minimumMoles: 0.1f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Freon

		public static readonly GasReactions FreonForm = new GasReactions(

			gasCreated: Gas.Freon,

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
			maximumTemperature:10000000f,
			minimumPressure:0f,
			maximumPressure: 100000000f,
			minimumMoles: 0.1f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		#region Stimulum

		public static readonly GasReactions StimulumForm = new GasReactions(

			gasCreated: Gas.Stimulum,

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
			maximumTemperature:10000000f,
			minimumPressure:0f,
			maximumPressure: 100000000f,
			minimumMoles: 0.1f,
			maximumMoles:10000000000f,
			energyChange: 0f
		);

		#endregion

		//Gas and minimum moles to react
		public Dictionary<Gas, GasReactionData> GasReactionData;

		public Gas GasCreated;

		public Reaction Reaction;

		public float MinimumTemperature;
		public float MaximumTemperature;

		public float MinimumPressure;
		public float MaximumPressure;

		public float MinimumMoles;
		public float MaximumMoles;

		public float EnergyChange;

		public readonly int Index;

		private GasReactions(Dictionary<Gas, GasReactionData> gasReactionData, Gas gasCreated, Reaction reaction, float minimumTemperature, float maximumTemperature, float minimumPressure, float maximumPressure, float minimumMoles, float maximumMoles, float energyChange)
		{
			GasReactionData = gasReactionData;

			GasCreated = gasCreated;

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