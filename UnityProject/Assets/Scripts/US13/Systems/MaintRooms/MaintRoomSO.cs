using System;
using System.Collections.Generic;
using UnityEngine;

namespace US13.Systems.MaintRooms
{
	[Serializable, CreateAssetMenu(fileName = "MaintRoomSO", menuName = "ScriptableObjects/MaintRoomSO")]
	public class MaintRoomSO : ScriptableObject
	{
		public DirectionFlag DoorDirections = DirectionFlag.Down;
		public string roomFileName;
		[Tooltip("The maximum number of these rooms that can be chosen to spawn in any given round.")]
		public uint maximumCopies = 99;

		//Keeps track of the amount of each roomtype that has been created. Resets on round start.
		private static Dictionary<MaintRoomSO, uint> roomCopies = new Dictionary<MaintRoomSO, uint>();

		/// <summary>
		/// Registers a room type as having spawned for the purpose of a maximum number of copies.
		/// </summary>
		/// <param name="room">The roomSO instance that is trying to be selected</param>
		/// <returns>Wether or not this room should be permitted to spawn</returns>
		public static bool TryRegisterRoomAsSpawned(MaintRoomSO room)
		{
			if (roomCopies.ContainsKey(room) == false)
			{
				roomCopies.Add(room, 1);
				return true;
			}

			if (roomCopies[room] >= room.maximumCopies) return false;
			roomCopies[room] += 1;
			return true;
		}

		public static void ResetRoomCounters()
		{
			roomCopies = new Dictionary<MaintRoomSO, uint>();
		}
	}

	[Flags]
	public enum DirectionFlag
	{
		Up = 1,
		Down = 2,
		Left = 4,
		Right = 8
	}
}
