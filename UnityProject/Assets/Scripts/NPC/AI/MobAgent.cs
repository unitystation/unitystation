using UnityEngine;
using MLAgents;

/// <summary>
/// Handles the underlying logic for
/// the Mob[Brain] behaviours
/// </summary>
[RequireComponent(typeof(CustomNetTransform))]
[RequireComponent(typeof(RegisterObject))]
public class MobAgent : Agent
{
	protected CustomNetTransform cnt;
	protected RegisterObject registerObj;

	private Vector3 startPos;

	protected bool isServer;

	void Awake()
	{
		cnt = GetComponent<CustomNetTransform>();
		registerObj = GetComponent<RegisterObject>();
		agentParameters.onDemandDecision = true;
	}

	//Reset is used mainly for training
	//SetPosition() has now been commented out
	//as it was used in training. Leaving the
	//lines present for any future retraining
	public override void AgentReset()
	{
			//cnt.SetPosition(startPos);
	}

	public override void OnEnable()
	{
		//only needed for starting via a map scene through the editor:
		if (CustomNetworkManager.Instance == null) return;

		if (CustomNetworkManager.Instance._isServer)
		{
			cnt.OnTileReached().AddListener(OnTileReached);
			UpdateManager.Instance.Add(UpdateMe);
			startPos = transform.position;
			isServer = true;
			base.OnEnable();
			AgentServerStart();
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (isServer)
		{
			cnt.OnTileReached().RemoveListener(OnTileReached);
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	protected virtual void OnTileReached(Vector3Int tilePos) { }

	protected virtual void UpdateMe() { }

	/// <summary>
	/// Convenience method for when the bot has been initialized
	/// successfully on the server side
	/// </summary>
	protected virtual void AgentServerStart(){}
}