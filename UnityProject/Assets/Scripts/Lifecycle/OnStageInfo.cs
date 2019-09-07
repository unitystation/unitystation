
using UnityEngine;

/// <summary>
/// Holds details about how an object is being spawned. This is mostly
/// a placeholder which prevents having to change the signature of the lifecycle interfaces in the event
/// that some component ends up needing some extra info about how it is being spawned.
/// </summary>
public class OnStageInfo
{
	private static OnStageInfo defaultInstance = new OnStageInfo(null);
	//note: currently no data is needed but fields may be added later
	public readonly GameObject ClonedFrom;
	public bool IsCloned => ClonedFrom != null;

	private OnStageInfo(GameObject clonedFrom)
	{
		ClonedFrom = clonedFrom;
	}

	public static OnStageInfo Default()
	{
		return defaultInstance;
	}

	public static OnStageInfo Cloned(GameObject clonedFrom)
	{
		return new OnStageInfo(clonedFrom);
	}
}
