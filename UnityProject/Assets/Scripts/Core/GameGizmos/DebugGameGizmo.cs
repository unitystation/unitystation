using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGameGizmo : MonoBehaviour
{
	public GameObject TrackingFrom;

	public GameObject TrackingTo;

	public float Thickness = 0.03125f;
	public Color Colour = Color.white;

	public Vector3 LineStart;
	public Vector3 LineEnd;


	[NaughtyAttributes.Button]
	public void AddLine()
	{
		GameGizmomanager.AddNewLineStatic(TrackingFrom, LineStart, TrackingTo, LineEnd, Colour, Thickness);
	}
}
