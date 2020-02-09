﻿using System.Collections.Generic;
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
		/// True if the atmos simulation has no updates to perform
		/// </summary>
		public bool IsIdle => updateList.IsEmpty;

		/// <summary>
		/// Number of updates remaining for the atmos simulation to process
		/// </summary>
		public int UpdateListCount => updateList.Count;

		/// <summary>
		/// Holds the nodes which are being processed during the current Update
		/// </summary>
		private List<MetaDataNode> nodes = new List<MetaDataNode>(5);

		/// <summary>
		/// MetaDataNodes that we are requested to update but haven't yet
		/// </summary>
		private UniqueQueue<MetaDataNode> updateList = new UniqueQueue<MetaDataNode>();

		public bool IsInUpdateList(MetaDataNode node)
		{
			return updateList.Contains(node);
		}

		public void AddToUpdateList(MetaDataNode node)
		{
			updateList.Enqueue(node);
		}

		public void ClearUpdateList()
		{
			nodes.Clear();
			updateList = new UniqueQueue<MetaDataNode>();
		}

		public void Run()
		{
			int count = updateList.Count;
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
			//Gases are frozen within closed airlocks or walls
			if (node.IsOccupied || node.IsClosedAirlock)
			{
				return;
			}

			nodes.Clear();
			nodes.Add(node);

			node.AddNeighborsToList(ref nodes);

			bool isPressureChanged = AtmosUtils.IsPressureChanged(node, out var windDirection, out var windForce);

			if (isPressureChanged)
			{
				node.ReactionManager.AddWindEvent(node, windDirection, windForce);
				Equalize();

				for (int i = 1; i < nodes.Count; i++)
				{
					updateList.Enqueue(nodes[i]);
				}
			}
		}

		/// <summary>
		/// The thing that actually equalises the tiles
		/// </summary>
		private void Equalize()
		{
			// If there is just one isolated tile, it's not nescessary to calculate the mean.  Speeds up things a bit.
			if (nodes.Count > 1)
			{
				//Calculate the average gas from adding up all the adjacent tiles and dividing by the number of tiles
				GasMix MeanGasMix = CalcMeanGasMix();

				for (var i = 0; i < nodes.Count; i++)
				{
					MetaDataNode node = nodes[i];

					if (!node.IsOccupied)
					{
						node.GasMix = CalcAtmos(node.GasMix, MeanGasMix);

						if (node.IsSpace)
						{
							//Set to 0 if space
							node.GasMix *= 0;
						}
					}
				}
			}
		}

		private GasMix meanGasMix = new GasMix(GasMixes.Space);

		/// <summary>
		/// Calculate the average Gas tile if you averaged all the adjacent ones and itself
		/// </summary>
		/// <returns>The mean gas mix.</returns>
		private GasMix CalcMeanGasMix()
		{
			meanGasMix.Copy(GasMixes.Space);

			int targetCount = 0;

			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

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
					//Decay if occupied
					node.GasMix *= 0;
				}
			}

			// Sometime, we calculate the meanGasMix of a tile surrounded by IsOccupied tiles (no atmos)
			// This condition is to avoid a divide by zero error (or 0 / 0 that gives NaN)
			if (targetCount != 0)
			{
				for (int j = 0; j < Gas.Count; j++)
				{
					meanGasMix.Gases[j] /= targetCount;
				}

				meanGasMix.Pressure /= targetCount;
			}

			return meanGasMix;
		}

		private GasMix CalcAtmos(GasMix atmos, GasMix gasMix)
		{
			//Used for updating tiles with the averagee Calculated gas
			for (int i = 0; i < Gas.Count; i++)
			{
				atmos.Gases[i] = gasMix.Gases[i];
			}

			atmos.Pressure = gasMix.Pressure;

			return atmos;
		}
	}
}