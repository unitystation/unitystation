using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Atmospherics
{
	public interface Reaction
	{
		bool Satisfies(GasMix gasMix);

		float React(ref GasMix gasMix, GasReactions gasReaction);
	}

	public struct GasReactions
	{
		private static List<GasReactions> gasReactions = new List<GasReactions>();

		//list of gas reactions:

		public static readonly GasReactions BZ = new GasReactions(

			gasCreated: Gas.BZ,

			reaction: new BZFormationReaction(),

			gasReactionData: new Dictionary<Gas, GasReactionData>()
			{
				{
					Gas.NitrousOxide,
					new GasReactionData()
					{
						minimumMolesToReact = 0.1f,
						ratio = 1f
					}
				},

				{
					Gas.Plasma,
					new GasReactionData()
					{
						minimumMolesToReact = 0.1f,
						ratio = 1f
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
		public float ratio;
	}
}