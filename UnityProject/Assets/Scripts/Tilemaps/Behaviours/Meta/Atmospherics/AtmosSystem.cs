using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using UnityEngine;
using Mirror;

namespace Systems.Atmospherics
{
	public class AtmosSystem : SubsystemBehaviour
	{
		public override SystemType SubsystemType => SystemType.AtmosSystem;

		[SerializeField]
		private GasMixesSO defaultRoomGasMixOverride = null;

		private Dictionary<int, RoomGasSetter> toSet = new Dictionary<int, RoomGasSetter>();

		[Server]
		public override void Initialize()
		{
			//We have bool to stop null checks on every pos
			var hasCustomMix = defaultRoomGasMixOverride != null;

			foreach (var gasSetter in GetComponentsInChildren<RoomGasSetter>())
			{
				gasSetter.SetUp();
			}

			BoundsInt bounds = metaTileMap.GetBounds();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				//Get top tile at pos to check if it should spawn with no air
				bool spawnWithNoAir = false;

				var topTile = metaTileMap.GetTile(position, LayerTypeSelection.Effects | LayerTypeSelection.Underfloor);
				if (topTile is BasicTile tile)
				{
					spawnWithNoAir = tile.SpawnWithNoAir;
				}

				MetaDataNode node = metaDataLayer.Get(position, false);
				if ((node.IsRoom || node.IsOccupied) && !spawnWithNoAir)
				{
					//Check to see if theres a special room mix
					if (toSet.Count > 0 && toSet.TryGetValue(node.RoomNumber, out var gasSetter))
					{
						node.GasMix = GasMix.NewGasMix(gasSetter.GasMixToSpawn);
					}
					//See if the whole matrix has a custom mix
					else if(hasCustomMix)
					{
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

			foreach (var gasSetter in toSet)
			{
				_ = Despawn.ServerSingle(gasSetter.Value.gameObject);
			}

			toSet.Clear();
		}

		public override void UpdateAt(Vector3Int localPosition)
		{
			AtmosThread.Enqueue(metaDataLayer.Get(localPosition));
		}

		#region Room Gas Setter

		public void AddToList(int room, RoomGasSetter toAdd)
		{
			if (toSet.ContainsKey(room))
			{
				Logger.LogError($"Room number: {room} was already added, cant add {toAdd.gameObject.ExpensiveName()}");
				return;
			}

			toSet.Add(room, toAdd);
		}

		/// <summary>
		/// Use to set a rooms gas after the round has started, otherwise do it in the initialisation
		/// </summary>
		/// <param name="roomNumber">Room number to fill</param>
		/// <param name="gasMixToUse">GasMix to fill room with</param>
		public void SetRoomGas(int roomNumber, GasMix gasMixToUse)
		{
			BoundsInt bounds = metaTileMap.GetBounds();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				MetaDataNode node = metaDataLayer.Get(position, false);
				if (node.IsRoom && node.RoomNumber == roomNumber)
				{
					node.GasMix = GasMix.NewGasMix(gasMixToUse);
				}
			}
		}

		#endregion
	}
}
