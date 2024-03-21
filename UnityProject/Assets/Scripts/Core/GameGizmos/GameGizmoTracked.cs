using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameGizmoTracked : GameGizmo
{

	public Vector3 Position
	{
		set
		{
			transform.position = value;
			position = value;
		}
	}
	protected Vector3 position;
	public GameObject TrackingObject;



	public void SetUp(Vector3 InPosition, GameObject InTrackingObject)
	{
		position = InPosition;
		TrackingObject = InTrackingObject;
		if (TrackingObject != null)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		if (TrackingObject != null)
		{
			transform.position = TrackingObject.transform.TransformPoint(position);
		}
		else
		{
			transform.position = position;
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
		if (TrackingObject != null)
		{
			transform.position =  TrackingObject.transform.TransformPoint(position);
		}

	}
}
