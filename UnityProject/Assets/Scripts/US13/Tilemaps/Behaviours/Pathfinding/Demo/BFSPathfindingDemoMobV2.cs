using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System;
using US13.Managers.UpdateManager;
using US13.Player;
using Util;

namespace US13.Tilemaps.Behaviours.Pathfinding.Demo
{
	public class BFSPathfindingDemoMobV2 : MonoBehaviour
	{
		[SerializeField] private float traversalTickTime = 0.15f;

		private List<Vector3Int> currentPath = new();
		private PlayerScript mob;

		private void Start()
		{
			UpdateManager.Add(CallbackType.UPDATE, CheckForInput);
			UpdateManager.Add(FollowPath, traversalTickTime);
			Debug.Log("BFS pathfinding Demo script added to " + gameObject.name);
		}

		private void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.UPDATE, CheckForInput);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowPath);
		}

		private PlayerScript GetCurrentMob()
		{
			if (mob != null) return mob;
			return GetComponent<PlayerScript>();
		}

		private void CheckForInput()
		{
			if (Input.GetKeyUp(KeyCode.B))
			{
				GetPathToFollow();
			}
		}

		private void GetPathToFollow()
		{
			mob = GetCurrentMob();
			if (mob == null) return;
			var matrix = mob.gameObject.GetMatrixRoot();
			var start = mob.gameObject.TileLocalPosition();
			currentPath = matrix.MetaDataLayer.Pathfinder.FromTo(matrix.MetaDataLayer.Nodes, start.To3Int(), MouseUtils.MouseToWorldPos().ToLocalInt(matrix));
			StartCoroutine(PathfindingUtils.Visualize(currentPath, start.To3Int()));
		}

		private void FollowPath()
		{
			if (currentPath == null || currentPath.Count == 0) return;
			var pos = currentPath[0];
			PathfindingUtils.ShoveMobToPosition(mob, pos, 12f);
			currentPath.RemoveAt(0);
		}
	}
}
