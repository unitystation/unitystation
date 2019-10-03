using UnityEngine;

/// <summary>
/// AI brain specifically trained to explore
/// the surrounding area for specific objects
/// </summary>
public class MobExplore : MobAgent
{
	//Add your targets as needed
	public enum Target{ food }
	public Target target;

	public bool performingDecision;
	public bool exploring;

	protected override void AgentServerStart()
	{
		exploring = true;
	}

	public override void CollectObservations()
	{

	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{

	}

	protected override void OnTileReached(Vector3Int tilePos)
	{
		if (performingDecision) performingDecision = false;
	}

	protected override void UpdateMe()
	{
		MonitorDecisionMaking();
	}

	void MonitorDecisionMaking()
	{
		if (!exploring || performingDecision) return;
		performingDecision = true;
		RequestDecision();
	}
}
