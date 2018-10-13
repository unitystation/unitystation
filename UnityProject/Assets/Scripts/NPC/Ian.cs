using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PathFinding;

public class Ian : MonoBehaviour
{
	private PathFinder pathFinder;
	private CustomNetTransform customNetTransform;

	public int xTarget;
	public int yTarget;

	private void Awake()
	{
		pathFinder = GetComponent<PathFinder>();
		customNetTransform = GetComponent<CustomNetTransform>();
	}

	[ContextMenu("TestPath")]
	private void TestTargetPath()
	{
		Debug.Log("Test path");
		pathFinder.FindNewPath(Vector2Int.RoundToInt(transform.localPosition), new Vector2Int(xTarget, yTarget),
			PathFound, NoPathFound);
		
	}

	private void PathFound(List<Node> path)
	{
		Debug.Log("PathFound. Steps: " + path.Count);
	}

	private void NoPathFound()
	{
		Debug.Log("No path found");
	}

}
