using System;
using System.Collections.Generic;
using Logs;
using ScriptableObjects.Atmospherics;
using UnityEngine;
using Mirror;
using Tiles;

namespace Systems.Atmospherics
{
	public class AtmosSystem : MatrixSystemBehaviour
	{
		public override SystemType SubsystemType => SystemType.AtmosSystem;

		[SerializeField]
		private GasMixesSO defaultRoomGasMixOverride = null;

		[SerializeField]
		private bool overriderTileSpawnWithNoAir;

		private Dictionary<int, RoomGasSetter> toSetRoom = new Dictionary<int, RoomGasSetter>();

		private Dictionary<Vector3Int, RoomGasSetter> toSetOccupied = new Dictionary<Vector3Int, RoomGasSetter>();

		public override void Initialize()
		{
			//FillRoomGas not called from here as the room setting up is now a coroutine
		}

		[Server]
		public void FillRoomGas()
		{
			//We have bool to stop null checks on every pos
			var hasCustomMix = defaultRoomGasMixOverride != null;

			foreach (var gasSetter in GetComponentsInChildren<RoomGasSetter>())
			{
				gasSetter.SetUp();
			}

			var bounds = metaTileMap.GetLocalBounds();

			foreach (Vector3Int position in bounds.allPositionsWithin())
			{
				try
				{
					SetPositionGas(position, hasCustomMix);
				}
				catch (Exception e)
				{
					Loggy.LogError(e.ToString());
				}
			}

			foreach (var gasSetter in toSetRoom)
			{
				_ = Despawn.ServerSingle(gasSetter.Value.gameObject);
			}

			foreach (var gasSetter in toSetOccupied)
			{
				_ = Despawn.ServerSingle(gasSetter.Value.gameObject);
			}

			toSetRoom.Clear();
			toSetOccupied.Clear();
		}

		private void SetPositionGas(Vector3Int position, bool hasCustomMix)
		{
			//Get top tile at pos to check if it should spawn with no air
			bool spawnWithNoAir = false;

			var topTile = metaTileMap.GetTile(position, LayerTypeSelection.Effects | LayerTypeSelection.AllUnderFloor);
			if (overriderTileSpawnWithNoAir == false && topTile is BasicTile tile)
			{
				spawnWithNoAir = tile.SpawnWithNoAir;
			}

			MetaDataNode node = metaDataLayer.Get(position, false);
			if ((node.IsRoom || node.IsOccupied) && !spawnWithNoAir)
			{
				//Check to see if theres a special room mix
				if (node.IsRoom && toSetRoom.Count > 0 && toSetRoom.TryGetValue(node.RoomNumber, out var roomGasSetter))
				{
					//We use ChangeGasMix here incase we need to add overlays, while the BaseAirMix and BaseSpaceMix can set directly since none of the gases in them do
					//Does come with a performance penalty
					//node.ChangeGasMix(GasMix.NewGasMix(gasSetter.GasMixToSpawn));

					//TODO: above commented out due to performance on load for lavaland, remove line below if solution is found
					node.GasMix = GasMix.NewGasMix(roomGasSetter.GasMixToSpawn);
				}
				//Check to see if theres a special occupied mix
				else if (node.IsOccupied && toSetOccupied.Count > 0 &&
				         toSetOccupied.TryGetValue(position, out var occupiedGasSetter))
				{
					//We use ChangeGasMix here incase we need to add overlays, while the BaseAirMix and BaseSpaceMix can set directly since none of the gases in them do
					//Does come with a performance penalty
					//node.ChangeGasMix(GasMix.NewGasMix(gasSetter.GasMixToSpawn));

					//TODO: above commented out due to performance on load for lavaland, remove line below if solution is found
					node.GasMix = GasMix.NewGasMix(occupiedGasSetter.GasMixToSpawn);
				}
				//See if the whole matrix has a custom mix
				else if (hasCustomMix)
				{
					//ChangeGasMix here too for the same reason as above
					//node.ChangeGasMix(GasMix.NewGasMix(defaultRoomGasMixOverride.BaseGasMix));

					//TODO: above commented out due to performance on load for lavaland, remove line below if solution is found
					node.GasMix = GasMix.NewGasMix(defaultRoomGasMixOverride.BaseGasMix);
				}
				//Default to air mix otherwise
				else
				{
					node.GasMix = GasMix.NewGasMix(GasMixes.BaseAirMix);
				}
			}
			else
			{
				node.GasMix = GasMix.NewGasMix(GasMixes.BaseSpaceMix);
			}
		}

		public override void UpdateAt(Vector3Int localPosition)
		{
			AtmosManager.Instance.UpdateNode(metaDataLayer.Get(localPosition));
		}

		#region Room Gas Setter

		public void AddToListRoom(int room, RoomGasSetter toAdd)
		{
			if (toSetRoom.ContainsKey(room))
			{
				Loggy.LogError($"Room number: {room} was already added, cant add {toAdd.gameObject.ExpensiveName()}, localPos: {toAdd.RegisterTile.LocalPosition}, matrix: {toAdd.RegisterTile.Matrix}");
				return;
			}

			toSetRoom.Add(room, toAdd);
		}

		public void AddToListOccupied(Vector3Int localPos, RoomGasSetter toAdd)
		{
			if (toSetOccupied.ContainsKey(localPos))
			{
				Loggy.LogError($"Local pos: {localPos} was already added, cant add {toAdd.gameObject.ExpensiveName()}");
				return;
			}

			toSetOccupied.Add(localPos, toAdd);
		}

		/// <summary>
		/// Use to set a rooms gas after the round has started, otherwise do it in the initialisation
		/// </summary>
		/// <param name="roomNumber">Room number to fill</param>
		/// <param name="gasMixToUse">GasMix to fill room with</param>
		public void SetRoomGas(int roomNumber, GasMix gasMixToUse)
		{
			var bounds = metaTileMap.GetLocalBounds();

			foreach (Vector3Int position in bounds.allPositionsWithin())
			{
				MetaDataNode node = metaDataLayer.Get(position, false);
				if (node.IsRoom && node.RoomNumber == roomNumber)
				{
					//Use ChangeGasMix to remove old gas overlays and add new overlays
					node.ChangeGasMix(GasMix.NewGasMix(gasMixToUse));
				}
			}
		}

		#endregion
	}
}
