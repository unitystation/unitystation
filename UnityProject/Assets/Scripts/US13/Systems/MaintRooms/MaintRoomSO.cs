using System;
using UnityEngine;

namespace US13.Systems.MaintRooms
{
	[Serializable, CreateAssetMenu(fileName = "MaintRoomSO", menuName = "ScriptableObjects/MaintRoomSO")]
	public class MaintRoomSO : ScriptableObject
	{
		public DirectionFlag DoorDirections = DirectionFlag.Down;
		public string roomFileName;
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
