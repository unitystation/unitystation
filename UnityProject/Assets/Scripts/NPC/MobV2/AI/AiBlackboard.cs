using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Mob.MobV2.AI
{
	public class AiBlackboard : MonoBehaviour
	{
		public UniversalObjectPhysics ObjectPhysics;
		public Rotatable Rotatable;
		public List<BlackboardData> BlackboardDatas = new List<BlackboardData>();

		[HideInInspector] public List<Vector3Int> Directions = new List<Vector3Int>()
		{
			new Vector3Int(1, 0, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(0, 1, 0),
			new Vector3Int(0, -1, 0),
		};

		private int PlayerMask;

		private void Awake()
		{
			PlayerMask = LayerMask.GetMask("Players");
		}
	}

	public class BlackboardData {}

	/// <summary>
	/// Keeps track of the player world position, can be extended to keep track of more player data.
	/// </summary>
	public class PlayerTarget : BlackboardData //Example data
	{
		public PlayerTarget(GameObject playerObject)
		{
			if (playerObject.TryGetComponent<PlayerScript>(out var player))
			{
				Script = player;
			}
			else
			{
				Logger.LogError("[AiBlackboard] - Not a player!!!");
				return;
			}
			PlayerObject = playerObject;
		}
		public GameObject PlayerObject;
		public PlayerScript Script;
		public Vector3Int PlayerWorldPos => PlayerObject.RegisterTile().WorldPositionServer;
	}
}