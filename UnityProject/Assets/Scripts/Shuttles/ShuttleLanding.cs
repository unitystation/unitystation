using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShuttleLanding : MonoBehaviour
{
	private MatrixMove matrixMove;

	private void Awake()
	{
		matrixMove = GetComponent<MatrixMove>();
	}

	public void LandShuttleOnGround(Vector3 teleportCoordinate)
	{
		matrixMove.IsForceStopped = true;
		matrixMove.StopMovement();
		//Maybe change collision type?
		//matrixMove.matrixColliderType =
		matrixMove.SetPosition(teleportCoordinate);
	}

	public void ShuttleMovedToSpace(Vector3 teleportCoordinate)
	{
		matrixMove.IsForceStopped = false;
		matrixMove.SetPosition(teleportCoordinate);
	}
}
#if Unity_Editor
[CustomEditor(typeof(ShuttleLanding))]
public class ShuttleLandingEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		EditorGUILayout.HelpBox("teleport: ", MessageType.Info);
		ShuttleLanding matrix = (ShuttleLanding) target;

		if (GUILayout.Button("teleport land"))
		{
			matrix.LandShuttleOnGround(LandingZoneManager.Instance.landingZones.Values.PickRandom());
		}

		if (GUILayout.Button("teleport space"))
		{
			matrix.ShuttleMovedToSpace(LandingZoneManager.Instance.spaceTeleportPoints.Values.PickRandom());
		}
	}
}
#endif