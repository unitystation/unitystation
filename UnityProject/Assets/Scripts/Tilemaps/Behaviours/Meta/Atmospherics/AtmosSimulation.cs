using System.Collections.Generic;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

namespace Atmospherics
{
	/// <summary>
	/// Main class which runs the atmospheric simulation for a given atmos thread. Since there is currently only
	/// one thread (At the time of writing), this means AtmosSimulation runs the simulation for the entire map.
	///
	/// AtmosSimulation operates on MetaDataNodes. There is one MetaDataNode per tile. The metadatanode describes
	/// the current state of the tile and is updated when AtmosSimulation updates it.
	///
	/// AtmosSimulation works by updating only what MetaDataNodes it is told to update, via AddToUpdateList. Once
	/// it updates, it doesn't do anything else to that MetaDataNode until it is told to again.
	/// </summary>
	public class AtmosSimulation
	{
		/// <summary>
		/// Interval (seconds) between updates of the simulation.
		/// </summary>
		public float Speed = 0.1f;

		/// <summary>
		/// True if the atmos simulation has no updates to perform
		/// </summary>
		public bool IsIdle => updateList.IsEmpty;

		/// <summary>
		/// Number of updates remaining for the atmos simulation to process
		/// </summary>
		public int UpdateListCount => updateList.Count;

		/// <summary>
		/// determines how much things change during an update, calculated based on the Speed and number of
		/// updates that currently need processing.
		/// </summary>
		private float factor;

		/// <summary>
		/// Holds the nodes which are being processed during the current Update
		/// </summary>
		private List<MetaDataNode> nodes = new List<MetaDataNode>(5);

		/// <summary>
		/// MetaDataNodes that we are requested to update but haven't yet
		/// </summary>
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
				if (updateList.TryDequeue(out MetaDataNode node))
				{
					Update(node);
				}
			}
		}

		private void Update(MetaDataNode node)
		{
			nodes.Clear();
			nodes.Add(node);

			node.AddNeighborsToList(ref nodes);

			if (node.IsOccupied || node.IsSpace || AtmosUtils.IsPressureChanged(node))
			{
				Equalize();

				for (int i = 1; i < nodes.Count; i++)
				{
					updateList.Enqueue(nodes[i]);
				}
			}
		}

		private void Equalize()
		{
			GasMix gasMix = CalcMeanGasMix();

			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

				if (!node.IsOccupied)
				{
					node.GasMix = CalcAtmos(node.GasMix, gasMix);
				}
			}
		}

		private GasMix meanGasMix = new GasMix(GasMixes.Space);

		private GasMix CalcMeanGasMix()
		{
			meanGasMix.Copy(GasMixes.Space);

			int targetCount = 0;

			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

				if (node.IsSpace)
				{
					node.GasMix *= 1 - factor;
				}

				for (int j = 0; j < Gas.Count; j++)
				{
					meanGasMix.Gases[j] += node.GasMix.Gases[j];
				}

				meanGasMix.Pressure += node.GasMix.Pressure;

				if (!node.IsOccupied)
				{
					targetCount++;
				}
				else
				{
					node.GasMix *= 1 - factor;

					if (node.GasMix.Pressure > AtmosConstants.MinPressureDifference)
					{
						updateList.Enqueue(node);
					}
				}
			}

			for (int j = 0; j < Gas.Count; j++)
			{
				meanGasMix.Gases[j] /= targetCount;
			}

			meanGasMix.Pressure /= targetCount;

			return meanGasMix;
		}


		private GasMix CalcAtmos(GasMix atmos, GasMix gasMix)
		{
			for (int i = 0; i < Gas.Count; i++)
			{
				atmos.Gases[i] += (gasMix.Gases[i] - atmos.Gases[i]) * factor;
			}

			atmos.Pressure += (gasMix.Pressure - atmos.Pressure) * factor;

			return atmos;
		}
	}
}