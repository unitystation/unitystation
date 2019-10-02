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
	private Vector3 startPos;

	void Awake()
	{
		agentParameters.onDemandDecision = true;
	}

	public override void AgentReset()
	{
		Debug.Log("Agent Reset!");
		cnt.SetPosition(startPos);
		rewardCache = 0f;
		decisionCount = 0;
		distanceCache = 0;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		cnt.OnTileReached().AddListener(OnTileReached);
		UpdateManager.Instance.Add(UpdateMe);
		startPos = transform.position;
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
		var curDist = Vector2.Distance(testTarget.transform.position, transform.position);
		if (distanceCache == 0)
		{
			distanceCache = curDist;
		}
		AddVectorObs(curDist / 100f);
		AddVectorObs(distanceCache / 100f);
		//Observe the direction to target
		AddVectorObs(((Vector2) (testTarget.transform.position - transform.position)).normalized);

		var curPos = registerObj.LocalPositionServer;

		var act = 0;
		//Observe adjacent tiles
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;
				act++;

				var checkPos = curPos;
				checkPos.x += x;
				checkPos.y += y;
				var passable = registerObj.Matrix.IsPassableAt(checkPos, true);

				//Record the passable observation:
				if (!passable)
				{
					//Is the path blocked because of a door?
					//Feed observations to AI
					DoorController tryGetDoor = registerObj.Matrix.GetFirst<DoorController>(checkPos, true);
					if (tryGetDoor == null)
					{
						if (checkPos == Vector3Int.RoundToInt(testTarget.localPosition))
						{
							AddVectorObs(true);
						}
						else
						{
							// if it is just a solid wall then remove the possibility of
							// using this direction as an action
							SetActionMask(0, act);
							AddVectorObs(false);
						}
					}
					else
					{
						//NPCs can open doors with no access restrictions
						if ((int) tryGetDoor.AccessRestrictions.restriction == 0)
						{
							AddVectorObs(true);
						}
						else
						{
							AddVectorObs(false);
							SetActionMask(0, act);
						}
					}
				}
				else
				{
					AddVectorObs(true);
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
			MonitorRewards();
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

			var dest = registerObj.LocalPositionServer + (Vector3Int) dirToMove;
			if (dest == Vector3Int.RoundToInt(testTarget.transform.localPosition))
			{
				SetReward(1f);
				Done();
				return;
			}

			if (!cnt.Push(dirToMove))
			{
				//Path is blocked try again
				performingDecision = false;
				MonitorRewards();
				DoorController tryGetDoor =
					registerObj.Matrix.GetFirst<DoorController>(
						registerObj.LocalPositionServer + (Vector3Int) dirToMove, true);
				if (tryGetDoor)
				{
					tryGetDoor.TryOpen(gameObject);
				}
			}
		}

		decisionCount++;
	}

	void OnTileReached(Vector3Int tilePos)
	{
		var compareDist = Vector2.Distance(testTarget.transform.position, transform.position);

		if (compareDist < distanceCache)
		{
			rewardCache += calculateReward(compareDist);
			distanceCache = compareDist;
		}

		MonitorRewards();

		if (compareDist < 0.5f)
		{
			Done();
			SetReward(2f);
		}

		if (performingDecision) performingDecision = false;
	}

	float calculateReward(float dist)
	{
		float reward = 0f;
		if (dist > 50f)
		{
			return reward;
		}
		else
		{
			reward = Mathf.Lerp(1f, 0f, dist / 50f);
		}

		return reward;
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