using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
	public class PathFinder : MonoBehaviour
	{
		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;
		private CustomNetTransform customNetTransform;
		public enum Status { idle, searching, navigating, waypointreached }
		public Status status;
		private Node goal;
		private bool pathFound = false;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			customNetTransform = GetComponent<CustomNetTransform>();
		}

		public void TryMoveToPosition(Vector2Int destination)
		{
			StartCoroutine(SearchForRoute(destination));
		}

		IEnumerator SearchForRoute(Vector2Int destination)
		{
			status = Status.searching;
			yield return YieldHelper.EndOfFrame;
		}
	}
}
