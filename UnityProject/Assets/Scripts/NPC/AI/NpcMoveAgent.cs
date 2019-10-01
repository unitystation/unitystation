using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

/// <summary>
/// AI Agent for NPC Movement decisions
/// Inherit NpcMoveAgent for customized behaviours
/// but keep the observations the same or you will need
/// to train a separate brain
/// </summary>
public class NpcMoveAgent : Agent
{
	public Transform testTarget;
	public CustomNetTransform cnt;
	public RegisterObject registerObj;
	public bool performingDecision = false;
	public bool move = false;
	private float rewardCache;
	public int rewardDay = 7;
	private int decisionCount = 0;
	private float distanceCache = 0;

	void Awake()
	{
		agentParameters.onDemandDecision = true;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		cnt.OnTileReached().AddListener(OnTileReached);
		UpdateManager.Instance.Add(UpdateMe);
		move = true;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		cnt.OnTileReached().RemoveListener(OnTileReached);
		UpdateManager.Instance.Remove(UpdateMe);
	}

	public override void CollectObservations()
	{
		distanceCache = Vector2.Distance(testTarget.transform.position, transform.position);

		//Observe the direction to target
		AddVectorObs(((Vector2) (testTarget.transform.position - transform.position)).normalized);

		var curPos = registerObj.LocalPositionServer;

		//Observe adjacent tiles
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;

				var checkPos = curPos;
				checkPos.x += x;
				checkPos.y += y;
				var passable = registerObj.Matrix.IsPassableAt(checkPos, true);

				//Record the passable observation:
				AddVectorObs(passable);

				if (!passable)
				{
					//Is the path blocked because of a door?
					//Feed observations to AI
					DoorController tryGetDoor;
					if (registerObj.Matrix.TryGetComponent(out tryGetDoor))
					{
						//	AddVectorObs(true);
					}
					else
					{
						//	AddVectorObs(false);
					}
				}
			}
		}
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}

	void PerformMoveAction(int act)
	{
		//0 = no move (1 - 8 are directions to move in reading from left to right)
		if (act == 0)
		{
			performingDecision = false;
		}
		else
		{
			Vector2Int dirToMove = Vector2Int.zero;
			int count = 1;
			for (int y = 1; y > -2; y--)
			{
				for (int x = -1; x < 2; x++)
				{
					if (count == act)
					{
						dirToMove.x += x;
						dirToMove.y += y;
						y = -100;
						break;
					}
					else
					{
						count++;
					}
				}
			}

			if (!cnt.Push(dirToMove))
			{
				//Path is blocked try again
				performingDecision = false;
			}
		}

		decisionCount++;
		MonitorRewards();
	}

	void OnTileReached(Vector3Int tilePos)
	{
		var compareDist = Vector2.Distance(testTarget.transform.position, transform.position);

		if (compareDist < distanceCache)
		{
			rewardCache += 0.1f;
		}

		if (compareDist < 0.5f)
		{
			Done();
			move = false;
		}

		if (performingDecision) performingDecision = false;
	}

	void MonitorRewards()
	{
		if (decisionCount >= rewardDay)
		{
			decisionCount = 0;
			SetReward(rewardCache);
			rewardCache = 0;
		}
	}

	void UpdateMe()
	{
		MonitorDecisionMaking();
	}

	void MonitorDecisionMaking()
	{
		if (!move || performingDecision) return;
		performingDecision = true;
		RequestDecision();
	}
}