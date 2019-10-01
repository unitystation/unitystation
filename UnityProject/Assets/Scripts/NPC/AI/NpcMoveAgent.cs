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
public class NpcMoveAgent : Agent, IOnStageServer
{
	public Transform testTarget;
	public CustomNetTransform cnt;
	public RegisterObject registerObj;
	private bool performingDecision = false;
	private bool alive = false;

	public void GoingOnStageServer(OnStageInfo info)
	{
		alive = true;
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		UpdateManager.Instance.Remove(UpdateMe);
	}

	public override void CollectObservations()
	{
		//Observe the direction to target
		AddVectorObs(((Vector2) (testTarget.transform.position - transform.position)).normalized);

		var curPos = registerObj.LocalPositionServer;

		//Observe adjacent tiles
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && x == 0) continue;
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
						AddVectorObs(true);
					}
					else
					{
						AddVectorObs(false);
					}
				}
			}
		}

		base.CollectObservations();
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		base.AgentAction(vectorAction, textAction);

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
			
		}
	}

	void UpdateMe()
	{
		MonitorDecisionMaking();
	}

	void MonitorDecisionMaking()
	{
		if (!alive || performingDecision) return;

		performingDecision = true;
		RequestDecision();
	}
}