using System.Collections;
using System.Collections.Generic;
using Core.Physics;
using SecureStuff;
using Systems.Scenes;
using UnityEditor;
using UnityEngine;

public class RandomExitPosition : MonoBehaviour
{

	public List<ExitMarker> ExitMarkers = new List<ExitMarker>();


	public void OnEnable()
	{
		EventManager.AddHandler(Event.RoundStarted, PostStart);
	}

	public void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundStarted, PostStart);
	}

	[ContextMenu("RebuildExitMarkerList"), VVNote(VVHighlight.SafeToModify100), NaughtyAttributes.Button]
	void RebuildExitMarkerList()
	{

		ExitMarkers.Clear();
		foreach (Transform t in transform.parent)
		{
			var mobSpawner = t.GetComponent<ExitMarker>();
			if (mobSpawner != null)
			{
				ExitMarkers.Add(mobSpawner);
			}
		}


#if UNITY_EDITOR
		EditorUtility.SetDirty(gameObject);
#endif
	}

	public void PostStart()
	{
		if (ExitMarkers.Count > 0)
		{
			this.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(ExitMarkers.PickRandom().gameObject.AssumedWorldPosServer()); //Randomise gateway position.
		}
	}
}
