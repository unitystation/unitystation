using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGizmoTracked : GameGizmo
{


	public Vector3 Position;
	public GameObject TrackingObject;


	public void SetUp(Vector3 InPosition, GameObject InTrackingObject)
	{
		Position = InPosition;
		TrackingObject = InTrackingObject;
		if (TrackingObject != null)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		if (TrackingObject != null)
		{
			transform.position = TrackingObject.AssumedWorldPosServer() + Position;
		}
		else
		{
			transform.position = Position;
		}

	}


	public void OnEnable()
	{
		if (TrackingObject != null)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

	}


	public void OnDisable()
	{
		if (TrackingObject != null)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}


	public void UpdateMe()
	{
		transform.position = TrackingObject.AssumedWorldPosServer() + Position;
	}
}
