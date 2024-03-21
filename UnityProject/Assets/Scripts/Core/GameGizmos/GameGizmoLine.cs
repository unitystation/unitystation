using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameGizmoLine : GameGizmo
{

	public LineRenderer Renderer;

	public GameObject TrackingFrom;
	public Vector3 From;

	public GameObject TrackingTo;
	public Vector3 To;


	public void SetUp(GameObject InTrackingFrom, Vector3 InFrom,   GameObject InTrackingTo, Vector3 InTo, Color color, float LineThickness)
	{
		TrackingFrom = InTrackingFrom;
		From = InFrom;

		TrackingTo = InTrackingTo;
		To = InTo;
		Renderer.startColor = color;
		Renderer.endColor = color;

		Renderer.startWidth = LineThickness;
		Renderer.endWidth = LineThickness;

		if (TrackingFrom != null || TrackingTo != null)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}


		if (TrackingFrom != null)
		{
			Renderer.SetPosition(0, TrackingFrom.transform.localToWorldMatrix * From);
		}
		else
		{
			Renderer.SetPosition(0, From);
		}

		if (TrackingTo != null)
		{
			Renderer.SetPosition(1, TrackingTo.transform.localToWorldMatrix *  To);
		}
		else
		{
			Renderer.SetPosition(1, To);
		}
	}

	public void OnEnable()
	{
		if (TrackingFrom != null || TrackingTo != null)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

	}


	public void OnDisable()
	{
		if (TrackingFrom != null || TrackingTo != null)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	public void UpdateMe()
	{
		if (TrackingFrom != null)
		{
			Renderer.SetPosition(0, TrackingFrom.transform.localToWorldMatrix * From);
		}
		else
		{
			Renderer.SetPosition(0, From);
		}

		if (TrackingTo != null)
		{
			Renderer.SetPosition(1, TrackingTo.transform.localToWorldMatrix *  To);
		}
		else
		{
			Renderer.SetPosition(1, To);
		}
	}


}
