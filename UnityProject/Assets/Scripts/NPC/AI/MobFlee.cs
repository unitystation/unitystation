using UnityEngine;

/// <summary>
/// AI brain specifically trained to flee
/// from a specific target
/// </summary>
public class MobFlee : MobAgent
{
	public Transform fleeTarget;
	private float distCache;

	protected override void AgentServerStart()
	{
		if (fleeTarget != null)
		{
		//	activated = true;
		}
	}

	public override void AgentReset()
	{
		base.AgentReset();
		distCache = Vector3.Distance(transform.position, fleeTarget.position);
	}

	public override void CollectObservations()
	{
		ObserveAdjacentTiles();
		var dist = Vector3.Distance(transform.position, fleeTarget.position);
		AddVectorObs(dist / 100f);
		//Observe the direction to target
		AddVectorObs(((Vector2) (fleeTarget.position - transform.position)).normalized);
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}

	protected override void OnTileReached(Vector3Int tilePos)
	{
		var dist = Vector3.Distance(transform.position, fleeTarget.position);
		if (dist > distCache)
		{
			distCache = dist;
			SetReward(calculateReward(dist));
		}
		else
		{
			SetReward(calculatePunishment(dist));
		}
		base.OnTileReached(tilePos);
	}

	float calculateReward(float dist)
	{
		return Mathf.Lerp(0, 10f, dist / 50f);
	}

	float calculatePunishment(float dist)
	{
		return Mathf.Lerp(-0.1f, 0f, dist / 100f);
	}
}