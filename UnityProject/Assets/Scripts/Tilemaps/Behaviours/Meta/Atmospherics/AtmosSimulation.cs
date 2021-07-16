using System.Collections.Generic;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using System;

namespace Systems.Atmospherics
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

			//Check to see if any reactions are needed
			DoReactions(node);

			//Check to see if node needs vfx applied
			GasVisualEffects(node);
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
				CalcMeanGasMix();

				for (var i = 0; i < nodes.Count; i++)
				{
					MetaDataNode node = nodes[i];

					if (!node.IsOccupied)
					{
						if (!node.IsSpace)
						{
							node.GasMix.Copy(meanGasMix);
						}
						else
						{
							node.GasMix.SetToEmpty();
						}
					}
				}
			}
		}

		private GasMix meanGasMix = GasMix.NewGasMix(GasMixes.BaseEmptyMix);

		/// <summary>
		/// Calculate the average Gas tile if you averaged all the adjacent ones and itself
		/// </summary>
		/// <returns>The mean gas mix.</returns>
		private void CalcMeanGasMix()
		{
			meanGasMix.Copy(GasMixes.BaseEmptyMix);

			var targetCount = 0;

			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

				if (node == null)
				{
					continue;
				}

				meanGasMix.Volume += node.GasMix.Volume;
				GasMix.TransferGas(meanGasMix, node.GasMix, node.GasMix.Moles);

				if (!node.IsOccupied)
				{
					targetCount++;
				}
				else
				{
					//Decay if occupied
					node.GasMix.SetToEmpty();
				}
			}

			// Sometimes, we calculate the meanGasMix of a tile surrounded by IsOccupied tiles (no atmos, ie: walls)
			if (targetCount == 0)
				return;

			meanGasMix.Volume /= targetCount; //Note: this assumes the volume of all tiles are the same
			foreach (var gasData in meanGasMix.GasesArray)
			{
				meanGasMix.GasData.SetMoles(gasData.GasSO, meanGasMix.GasData.GetGasMoles(gasData.GasSO) / targetCount);
			}
		}

		#region GasVisualEffects

		//Handles checking for vfx changes
		//If needed, sends them to a queue in ReactionManager so that main thread will apply them
		private void GasVisualEffects(MetaDataNode node)
		{
			if (node == null || node.ReactionManager == null)
			{
				return;
			}

			foreach (var gasData in node.GasMix.GasesArray)
			{
				var gas = gasData.GasSO;
				if(!gas.HasOverlay) continue;

				var gasAmount = node.GasMix.GetMoles(gas);

				if(gasAmount > gas.MinMolesToSee)
				{
					if(node.GasOverlayData.Contains(gas)) continue;

					node.AddGasOverlay(gas);

					node.ReactionManager.TileChangeManager.AddOverlay(node.Position, TileManager.GetTile(TileType.Effects, gas.TileName) as OverlayTile);
				}
				else
				{
					if(node.GasOverlayData.Contains(gas) == false) continue;

					node.RemoveGasOverlay(gas);

					node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(node.Position, LayerType.Effects, gas.OverlayType);
				}
			}
		}

		#endregion

		#region GasReactions

		private void DoReactions(MetaDataNode node)
		{
			if (node == null || node.ReactionManager == null)
			{
				return;
			}

			var gasMix = node.GasMix;

			foreach (var gasReaction in GasReactions.All)
			{
				if(ReactionMoleCheck(gasReaction, gasMix)) continue;

				if (gasMix.Temperature < gasReaction.MinimumTemperature || gasMix.Temperature > gasReaction.MaximumTemperature) continue;

				if (gasMix.Pressure < gasReaction.MinimumPressure || gasMix.Pressure > gasReaction.MaximumPressure) continue;

				if (gasMix.Moles < gasReaction.MinimumMoles || gasMix.Moles > gasReaction.MaximumMoles) continue;

				if (node.ReactionManager.reactions.TryGetValue(node.Position, out var gasHashSet) && gasHashSet.Contains(gasReaction)) continue;

				//If too much Hyper-Noblium theres no reactions!!!
				if(gasMix.GetMoles(Gas.HyperNoblium) >= AtmosDefines.REACTION_OPPRESSION_THRESHOLD) break;

				gasReaction.Reaction.React(gasMix, node);
			}
		}

		private static bool ReactionMoleCheck(GasReactions gasReaction, GasMix gasMix)
		{
			foreach (var data in gasReaction.GasReactionData)
			{
				if(gasMix.GetMoles(data.Key) == 0) return true;

				if(gasMix.GetMoles(data.Key) < data.Value.minimumMolesToReact) return true;
			}

			return false;
		}

		#endregion
	}
}