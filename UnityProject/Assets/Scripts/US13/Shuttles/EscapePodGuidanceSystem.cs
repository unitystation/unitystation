using System.Collections.Generic;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Shuttles;

public class EscapePodGuidanceSystem : AutopilotShipMachine
{
	public float MinLaunchDistance = 250f;
	public float MaxLaunchDistance = 400f;

	public static List<EscapePodGuidanceSystem> EscapePods = new List<EscapePodGuidanceSystem>();

	//Pods can't reverse, so only spread the heading a little either side of forward
	public float MaxLaunchAngle = 30f;

	public override void Start()
	{
		base.Start();
		EscapePods.Add(this);
	}

	public void OnDestroy()
	{
		EscapePods.Remove(this);
	}


	[NaughtyAttributes.Button]
	public void MoveToCentcom()
	{
		if (CustomNetworkManager.IsServer == false) return;
		MovedToAfterFinishingChain = null;
		MoveDirectionIn = false;
	}

	public override void ReachedEndOfOutBuoyChain(GuidanceBuoy GuidanceBuoy)
	{
		base.ReachedEndOfOutBuoyChain(GuidanceBuoy);
		MoveToFarPosition(GuidanceBuoy.transform.position);
	}

	private void MoveToFarPosition(Vector3 origin)
	{
		Vector2 forward = mm.transform.up;
		var angle = Random.Range(-MaxLaunchAngle, MaxLaunchAngle);
		var direction = (Vector2)(Quaternion.Euler(0, 0, angle) * forward);
		var distance = Random.Range(MinLaunchDistance, MaxLaunchDistance);

		MoveToPosition(origin + (Vector3)(direction * distance));
	}
}