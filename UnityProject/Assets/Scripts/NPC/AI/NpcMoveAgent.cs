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
		base.CollectObservations();
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		base.AgentAction(vectorAction, textAction);
		performingDecision = false;
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