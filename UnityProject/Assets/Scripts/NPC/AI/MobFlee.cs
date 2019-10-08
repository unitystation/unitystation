using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI brain specifically trained to flee
/// from a specific target
/// </summary>
public class MobFlee : MobAgent
{
	public Transform fleeTarget;

	protected override void AgentServerStart()
	{
		if (fleeTarget != null)
		{
			activated = true;
		}
	}

	public override void CollectObservations()
	{

	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}
}
