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
			nodes.Clear();
			nodes.Add(node);

			node.AddNeighborsToList(ref nodes);

			//Conduct heat from this nodes gas mix to this nodes tile
			if (node.IsOccupied == false || node.IsIsolatedNode)
			{
				ConductFromOpenToSolid(node, meanGasMix);
			}

			//Gases are frozen within walls and isolated tiles (closed airlocks) so dont do gas equalising
			if (node.IsOccupied == false && node.IsIsolatedNode == false)
			{
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

			//Check to see if we need to do conductivity to other adjacent tiles
			Conductivity(node);

			//Only allow open tiles or isolated tiles to do reactions
			if (node.IsOccupied && node.IsIsolatedNode == false) return;

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
			// If there is just one isolated tile, it's not necessary to calculate the mean.  Speeds up things a bit.
			if (nodes.Count <= 1)  return;

			//Calculate the average gas from adding up all the adjacent tiles and dividing by the number of tiles
			CalcMeanGasMix();

			MetaDataNode node;

			//Then equalise the open gas mixes
			for (var i = 0; i < nodes.Count; i++)
			{
				node = nodes[i];

				//If the node is not occupied then try to share out gas mix
				if (node.IsOccupied == false && node.IsIsolatedNode == false)
				{
					//If its not space then share otherwise it is space so set to empty
					if (node.IsSpace == false)
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

		#region Conductivity

		private void Conductivity(MetaDataNode currentNode)
		{
			//Only allow conducting if we are the starting node or we are allowed to
			if(currentNode.StartingSuperConduct == false && currentNode.AllowedToSuperConduct == false) return;

			//Starting node must have higher temperature
			if (currentNode.ConductivityTemperature < (currentNode.StartingSuperConduct
				? AtmosDefines.MINIMUM_TEMPERATURE_START_SUPERCONDUCTION
				: AtmosDefines.MINIMUM_TEMPERATURE_FOR_SUPERCONDUCTION))
			{

				//Disable node if it fails temperature check
				currentNode.AllowedToSuperConduct = false;
				currentNode.StartingSuperConduct = false;
				return;
			}

			if (currentNode.HeatCapacity < AtmosDefines.M_CELL_WITH_RATIO) return;

			currentNode.AllowedToSuperConduct = true;

			//Solid conductivity is done by meta data node variables, as walls dont have functioning gas mix
			SolidConductivity(currentNode);

			//Check to see whether we should disable the node
			if (currentNode.ConductivityTemperature < AtmosDefines.MINIMUM_TEMPERATURE_FOR_SUPERCONDUCTION)
			{
				//Disable node if it fails temperature check
				currentNode.AllowedToSuperConduct = false;
				currentNode.StartingSuperConduct = false;
			}
		}

		private void SolidConductivity(MetaDataNode currentNode)
		{
			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

				//Dont spread heat to self
				if(node == currentNode) continue;

				var tempDelta = currentNode.ConductivityTemperature;

				//Radiate temperature between Solid and Space
				if(node.IsSpace)
				{
					if(currentNode.ConductivityTemperature <= TemperatureUtils.ZERO_CELSIUS_IN_KELVIN) continue;

					tempDelta -= AtmosDefines.SPACE_TEMPERATURE;
					RadiateTemperatureToSpace(currentNode, node, tempDelta);
				}
				//Share temperature between Solid and Solid
				else
				{
					tempDelta -= node.ThermalConductivity;
					ConductFromSolidToSolid(currentNode, node, tempDelta);
				}
			}
		}

		#region Open To ....

		/// <summary>
		/// Transfers heat between an Open tile and a Solid tile
		/// Uses data from MetaDataNode for SolidNode and GasMix values for Open node
		/// </summary>
		private void ConductFromOpenToSolid(MetaDataNode solidNode, GasMix meanGasMix)
		{
			var tempDelta = solidNode.ConductivityTemperature - meanGasMix.Temperature;

			if (Mathf.Abs(tempDelta) <= AtmosDefines.MINIMUM_TEMPERATURE_DELTA_TO_CONSIDER) return;

			if (meanGasMix.WholeHeatCapacity <= AtmosConstants.MINIMUM_HEAT_CAPACITY) return;

			if(solidNode.HeatCapacity <= AtmosConstants.MINIMUM_HEAT_CAPACITY) return;

			//The larger the combined capacity the less is shared
			var heat = solidNode.ThermalConductivity * tempDelta *
			           (solidNode.HeatCapacity * meanGasMix.WholeHeatCapacity /
			            (solidNode.HeatCapacity + meanGasMix.WholeHeatCapacity));

			solidNode.ConductivityTemperature = Mathf.Max(
				solidNode.ConductivityTemperature - (heat / solidNode.HeatCapacity),
				AtmosDefines.SPACE_TEMPERATURE);

			meanGasMix.SetTemperature(Mathf.Max(
				meanGasMix.Temperature + (heat / meanGasMix.WholeHeatCapacity),
				AtmosDefines.SPACE_TEMPERATURE));

			//Do atmos update for the Solid node if temperature is allowed so it can do conduction
			//This is checking for the start temperature as this is how the cycle will begin
			if (solidNode.ConductivityTemperature < AtmosDefines.MINIMUM_TEMPERATURE_START_SUPERCONDUCTION) return;

			if (solidNode.AllowedToSuperConduct == false)
			{
				solidNode.AllowedToSuperConduct = true;

				//Allow this node to trigger other tiles super conduction
				solidNode.StartingSuperConduct = true;
			}

			AtmosManager.Update(solidNode);
		}

		#endregion

		#region Solid To ...

		/// <summary>
		/// Used to transfer heat between an Solid tile and Space
		/// Uses data from MetaDataNode for solid node and MetaDataNode date for Space node
		/// </summary>
		private void RadiateTemperatureToSpace(MetaDataNode currentNode, MetaDataNode openNode, float tempDelta)
		{
			if(currentNode.HeatCapacity <= 0) return;

			if(Mathf.Abs(tempDelta) <= AtmosDefines.MINIMUM_TEMPERATURE_DELTA_TO_CONSIDER) return;

			//The larger the combined capacity the less is shared
			var heat = openNode.ThermalConductivity * tempDelta *
			           (currentNode.HeatCapacity * AtmosDefines.SPACE_HEAT_CAPACITY /
			            (currentNode.HeatCapacity + AtmosDefines.SPACE_HEAT_CAPACITY));

			currentNode.ConductivityTemperature -= heat / currentNode.HeatCapacity;
		}

		/// <summary>
		/// Used to transfer heat between an Solid tile and Solid tile
		/// Uses data from MetaDataNode for the current solid node and MetaDataNode date for the other Solid node
		/// </summary>
		private void ConductFromSolidToSolid(MetaDataNode currentNode, MetaDataNode solidNode, float tempDelta)
		{
			if (Mathf.Abs(tempDelta) <= AtmosDefines.MINIMUM_TEMPERATURE_DELTA_TO_CONSIDER) return;

			//The larger the combined capacity the less is shared
			var heat = solidNode.ThermalConductivity * tempDelta *
			           (currentNode.HeatCapacity * solidNode.HeatCapacity /
			            (currentNode.HeatCapacity + solidNode.HeatCapacity));

			//The higher your own heat cap the less heat you get from this arrangement
			currentNode.ConductivityTemperature -= heat / currentNode.HeatCapacity;
			solidNode.ConductivityTemperature += heat / solidNode.HeatCapacity;

			//Do atmos update for the next solid node if temperature is allowed so it can do conduction
			if(solidNode.ConductivityTemperature < AtmosDefines.MINIMUM_TEMPERATURE_FOR_SUPERCONDUCTION) return;
			solidNode.AllowedToSuperConduct = true;
			AtmosManager.Update(solidNode);
		}

		#endregion

		#endregion

		private GasMix meanGasMix = GasMix.NewGasMix(GasMixes.BaseEmptyMix);

		/// <summary>
		/// Calculate the average Gas tile if you averaged all the adjacent ones and itself
		/// </summary>
		/// <returns>The mean gas mix.</returns>
		private void CalcMeanGasMix()
		{
			meanGasMix.Clear();
			
			var targetCount = 0;

			for (var i = 0; i < nodes.Count; i++)
			{
				MetaDataNode node = nodes[i];

				if (node == null)
				{
					continue;
				}

				//If node is not occupied then we want to add it to the total
				if (node.IsOccupied == false && node.IsIsolatedNode == false)
				{
					meanGasMix.Volume += node.GasMix.Volume;
					GasMix.TransferGas(meanGasMix, node.GasMix, node.GasMix.Moles, true);
					targetCount++;
				}
				else if(node.IsIsolatedNode == false)
				{
					//Remove all overlays for occupied tiles
					RemovalAllGasOverlays(node);

					//We trap the gas in the walls to stop instances where you remove a wall and theres a vacuum there
				}
			}

			// Sometimes, we calculate the meanGasMix of a tile surrounded by IsOccupied tiles (no atmos, ie: walls)
			if (targetCount == 0) return;

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

		private void RemovalAllGasOverlays(MetaDataNode node)
		{
			if (node == null || node.ReactionManager == null) return;

			foreach (var gas in node.GasOverlayData)
			{
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(node.Position, LayerType.Effects, gas.OverlayType);
			}

			node.GasOverlayData.Clear();
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