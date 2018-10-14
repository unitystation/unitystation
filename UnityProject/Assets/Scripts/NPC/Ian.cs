using System.Collections;
using System.Collections.Generic;
using PathFinding;
using UnityEngine;
using UnityEngine.Networking;

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

	//Test can only be performed from Inspector. Right click the component and choose TestPath at runtime
	[ContextMenu("TestPath")]
	private void TestTargetPath()
	{
		customNetTransform.enabled = false;

		if (testStatus == TestStatus.Idle)
		{
			Debug.Log("Test path");
			index = 0;
			FindPath();
		}
	}

	private void FindPath()
	{
		testStatus = TestStatus.FindingPath;
		pathFinder.FindNewPath(Vector2Int.RoundToInt(transform.localPosition), TestTargetWaypoints[index],
			PathFound, NoPathFound);
	}

	private void PathFound(List<Node> path)
	{
		Debug.Log("PathFound. Steps: " + path.Count);
		StartCoroutine(TestMovement(path));
		testStatus = TestStatus.FollowingPath;
	}

	private void NoPathFound()
	{
		Debug.Log("No path found");
		testStatus = TestStatus.Idle;
	}

	IEnumerator TestMovement(List<Node> path)
	{
		yield return YieldHelper.EndOfFrame;
		//For visual purposes only, not networked synced and CNT is switched off until changes
		//can be made for path following in the near future:

		for (int i = 0; i < path.Count; i++)
		{
			CheckSpriteDirection((float) path[i].position.x - transform.localPosition.x);

			while (new Vector2(path[i].position.x, path[i].position.y) != (Vector2) transform.localPosition)
			{
				transform.localPosition =
					Vector3.MoveTowards(transform.localPosition, new Vector2(path[i].position.x, path[i].position.y), 2f * Time.deltaTime);
				yield return YieldHelper.EndOfFrame;
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