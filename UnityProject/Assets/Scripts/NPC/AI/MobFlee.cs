using UnityEngine;

/// <summary>
/// AI brain specifically trained to flee
/// from a specific target
/// </summary>
public class MobFlee : MobAgent
{
	public Transform fleeTarget;
	private float lastDist;

	protected override void AgentServerStart()
	{
		if (fleeTarget != null)
		{
			activated = true;
		}
	}

	public override void AgentReset()
	{
		base.AgentReset();
		lastDist = 0f;
	}

	public override void CollectObservations()
	{
		ObserveAdjacentTiles();
		var dist = Vector3.Distance(transform.position, fleeTarget.position);
		AddVectorObs(dist);
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}

	protected override void OnTileReached(Vector3Int tilePos)
	{
		var dist = Vector3.Distance(transform.position, fleeTarget.position);
		if (dist > lastDist)
		{
			lastDist = dist;
			SetReward(calculateReward(dist));
		}
		base.OnTileReached(tilePos);
	}

	float calculateReward(float dist)
	{
		float reward = 0f;
		if (dist > 100f)
		{
			return 1f;
		}
		else
		{
			reward = Mathf.Lerp(0, 1f, dist / 100f);
		}

		return reward;
	}
}