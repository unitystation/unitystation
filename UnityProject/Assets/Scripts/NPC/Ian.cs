﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PathFinding;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;

///<summary>
/// README:
/// Ian is currently being used as our test subject for the Pathfinding development
///</summary>

public class Ian : MonoBehaviour
{
	private PathFinder pathFinder;
	private CustomNetTransform customNetTransform;

	public Vector2Int[] TestTargetWaypoints;
	int index = 0;
	public enum TestStatus
	{
		Idle,
		FindingPath,
		FollowingPath
	}

	public TestStatus testStatus; //for visual status on the inspector at runtime

	public GameObject spriteObj;

	private void Awake()
	{
		pathFinder = GetComponent<PathFinder>();
		customNetTransform = GetComponent<CustomNetTransform>();
	}

	[ContextMenu("TestPathMany")]
	private void TestTargetPathMany()
	{
		var sw = new System.Diagnostics.Stopwatch();
		var times = new List<float>();
		List<Node> path = null;
		for (int i = 0; i < 100; ++i) {
			sw.Restart();
			path = pathFinder.FindNewPath(Vector2Int.RoundToInt(transform.localPosition), TestTargetWaypoints[index]);
			times.Add(sw.ElapsedMilliseconds);
		}
		times.Sort();
		Logger.LogFormat("Finished running {amount} times...\n" +
		"Mean: {mean}ms\n" +
		"Median: {median}ms\n" +
		"Best: {best}ms\n" +
		"Worst: {worst}ms", Category.NetMessage,
		args: new
		{
			amount = times.Count,
			mean = times.Average(),
			media = times[times.Count / 2],
			best = times[0],
			worst = times[times.Count - 1]
		});

		if (path != null)
			PathFound(path);
	}

	//Test can only be performed from Inspector. Right click the component and choose TestPath at runtime
	[ContextMenu("TestPath")]
	private void TestTargetPath() {
		customNetTransform.enabled = false;

		if (testStatus == TestStatus.Idle) {
			Logger.Log("Testing target path", Category.NetMessage);
			index = 0;
			FindPath();
		}
	}

	private void FindPath()
	{
		testStatus = TestStatus.FindingPath;
		var path = pathFinder.FindNewPath(Vector2Int.RoundToInt(transform.localPosition), TestTargetWaypoints[index]);
		if (path == null) NoPathFound();
		else PathFound(path);
	}

	private void PathFound(List<Node> path)
	{
		Logger.Log("Path found. Steps: " + path.Count, Category.NetMessage);
		StartCoroutine(TestMovement(path));
		testStatus = TestStatus.FollowingPath;
	}

	private void NoPathFound()
	{
		Logger.Log("No path found", Category.NetMessage);
		testStatus = TestStatus.Idle;
	}

	private List<Node> _path;
	void OnDrawGizmos() {
		if (_path != null) {
			for(var i = 1; i < _path.Count; ++i) {
				var from = new Vector3(_path[i - 1].position.x + 1, _path[i - 1].position.y + 1, 0);
				var to = new Vector3(_path[i].position.x + 1, _path[i].position.y + 1, 0);
				Gizmos.DrawLine(from, to);
			}
		}
	}

	IEnumerator TestMovement(List<Node> path)
	{
		_path = path;
		yield return WaitFor.EndOfFrame;
		//For visual purposes only, not networked synced and CNT is switched off until changes
		//can be made for path following in the near future:

		for (int i = 0; i < path.Count; i++)
		{
			CheckSpriteDirection((float) path[i].position.x - transform.localPosition.x);

			while (new Vector2(path[i].position.x, path[i].position.y) != (Vector2) transform.localPosition)
			{
				transform.localPosition =
					Vector3.MoveTowards(transform.localPosition, new Vector2(path[i].position.x, path[i].position.y), 2f * Time.deltaTime);
				yield return WaitFor.EndOfFrame;
			}
		}
		index++;
		if (index >= TestTargetWaypoints.Length)
		{
			testStatus = TestStatus.Idle;
		}
		else
		{
			FindPath();
		}
	}

	private void CheckSpriteDirection(float xFacing)
	{
		if (xFacing > 0f && spriteObj.transform.localScale.x > 0f)
		{
			FlipSpriteObj();
			return;
		}
		if (xFacing < 0f && spriteObj.transform.localScale.x < 0f)
		{
			FlipSpriteObj();
			return;
		}
	}

	void FlipSpriteObj()
	{
		var newScale = spriteObj.transform.localScale;
		newScale.x *= -1f;
		spriteObj.transform.localScale = newScale;
	}
}