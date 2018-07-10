using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Meta.Data;

namespace Tilemaps.Behaviours.Meta
{
	public static class AtmosUtils
	{
		public static void SetEmpty(MetaDataNode node)
		{
			node.Atmos.AirMix = new float[Gas.Count];
			node.Atmos.Temperature = 2.7f;
			node.Atmos.Moles = 0.000000000000281f;
		}
		
		public static void SetAir(MetaDataNode node)
		{
			node.Atmos.AirMix = new float[Gas.Count];
			node.Atmos.AirMix[Gas.Oxygen.Index] = 16.628484400890768491815384755837f;
			node.Atmos.AirMix[Gas.Nitrogen.Index] = 66.513937603563073967261539023347f;
			node.Atmos.Temperature = 293.15f;
			node.Atmos.Moles = 83.142422004453842459076923779184f;
		}

		public static void SetAirMixes(float[] mix, float[] jm, List<MetaDataNode> nodes)
		{
			var moles = 0.0f;
			var temperature = 0.0f;
			var count = 0;

			// Calculate new values for first node
			for (int i = 0; i < Gas.Count; i++)
			{
				if (mix[i] > 0)
				{
					temperature += jm[i] / (Gas.Get(i).HeatCapacity * mix[i]);
					mix[i] /= nodes.Count;
					moles += mix[i];
					count++;
				}
			}

			temperature /= count;

			// Copy to other nodes
			for (int i = 0; i < nodes.Count; i++)
			{
				// TODO own airMix array which is just referenced and not copied ?
				Array.Copy(mix, nodes[i].Atmos.AirMix, Gas.Count);
				nodes[i].Atmos.Temperature = temperature;
				nodes[i].Atmos.Moles = moles;
			}
		}
	}
}