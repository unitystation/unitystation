using System;
using System.Collections;
using System.Collections.Generic;
using InGameGizmos;
using UnityEngine;

namespace Tilemaps.Behaviours.Pathfinding
{
	public static class PathfindingUtils
	{
		public static IEnumerator Visualize(List<Vector3Int> traversalPath, Vector3Int start)
		{
			if (traversalPath == null || traversalPath.Count == 0) yield break;
			var startingVector = start;
			Color color = new Color(0,0,0);
			foreach (var p in traversalPath)
			{
				GameGizmomanager.AddNewLineStaticClient(null, startingVector, null,
					p, color, LineThickness: 0.03125f, 5f);
				float t = Mathf.PingPong(Time.time, 1);
				color = new Color(t, t, t);
				startingVector = p;
				yield return WaitFor.Seconds(0.075f);
			}
		}

		public static void ShoveMobToPosition(PlayerScript mob, Vector3Int position, float force)
		{
			Vector3Int direction = position - mob.gameObject.TileLocalPosition().To3Int();
			direction = new Vector3Int(
				Mathf.RoundToInt(direction.Normalize().x),
				Mathf.RoundToInt(direction.Normalize().y),
				Mathf.RoundToInt(direction.Normalize().z)
			);
			Debug.Log(direction);
			mob.playerMove.TryTilePush(direction.To2Int(), mob.gameObject, force);
		}
	}
}