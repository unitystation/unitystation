using System.Collections.Generic;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

namespace Atmospherics
{
	public class AtmosSimulation
	{
		public float Speed = 0.1f;

		public bool IsIdle => updateList.IsEmpty;

		public int UpdateListCount => updateList.Count;

		private float factor;
		private MetaDataNode[] nodes = new MetaDataNode[5];
		private UniqueQueue<MetaDataNode> updateList = new UniqueQueue<MetaDataNode>();

		public void AddToUpdateList(MetaDataNode node)
		{
			updateList.Enqueue(node);
		}

		public void Run()
		{
			int count = updateList.Count;

			factor = Mathf.Clamp(Speed * count / 100f, 0.01f, 1f);

			for (int i = 0; i < count; i++)
			{
				MetaDataNode node;
				if (updateList.TryDequeue(out node))
				{
					Update(node);
				}
			}
		}

		private void Update(MetaDataNode node)
		{
			nodes[0] = node;

			List<MetaDataNode> neighbors = node.Neighbors;

			for (int i = 0; i < neighbors.Count; i++)
			{
				nodes[1 + i] = neighbors[i];
			}

			if (node.IsOccupied || node.IsSpace || AtmosUtils.IsPressureChanged(node))
			{
				Equalize(1 + neighbors.Count);

				updateList.EnqueueAll(neighbors);
			}
		}

		private void Equalize(int nodesCount)
		{
			GasMix gasMix = CalcMeanGasMix(nodesCount);

			for (var i = 0; i < nodesCount; i++)
			{
				MetaDataNode node = nodes[i];

				if (!node.IsOccupied)
				{
					node.Atmos = CalcAtmos(node.Atmos, gasMix);
				}
			}
		}

		private GasMix CalcMeanGasMix(int nodesCount)
		{
			int targetCount = 0;

			float[] gases = new float[Gas.Count];
			float pressure = 0f;

			for (var i = 0; i < nodesCount; i++)
			{
				MetaDataNode node = nodes[i];

				if (node.IsSpace)
				{
					node.Atmos *= 1 - factor;
				}

				for (int j = 0; j < Gas.Count; j++)
				{
					gases[j] += node.Atmos.Gases[j];
				}

				pressure += node.Atmos.Pressure;

				if (!node.IsOccupied)
				{
					targetCount++;
				}
				else
				{
					node.Atmos *= 1 - factor;

					if (node.Atmos.Pressure > AtmosUtils.MinimumPressure)
					{
						updateList.Enqueue(node);
					}
				}
			}

			for (int j = 0; j < Gas.Count; j++)
			{
				gases[j] /= targetCount;
			}

			GasMix gasMix = GasMix.FromPressure(gases, pressure / targetCount);
			return gasMix;
		}

		private GasMix CalcAtmos(GasMix atmos, GasMix gasMix)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = atmos.Gases[i] + (gasMix.Gases[i] - atmos.Gases[i]) * factor;
			}

			float pressure = atmos.Pressure + (gasMix.Pressure - atmos.Pressure) * factor;

			return GasMix.FromPressure(gases, pressure, atmos.Volume);
		}
	}
}