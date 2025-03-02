using System;
using System.Linq;
using AddressableReferences;
using Core.Physics;
using Logs;
using Managers;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Mapping.Move
{
	[RequireComponent(typeof(RegisterItem))]
	public class LocationMarker : MonoBehaviour, IServerSpawn, IServerDespawn
	{
		public int LocationID = 0;
		public RegisterItem Register;

		private Vector3Int badPosition = new Vector3Int(0, 0, -100);

		private void Awake()
		{
			Register ??= GetComponent<RegisterItem>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			GameManager.Instance.AddLocationMarker(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			GameManager.Instance.RemoveLocationMarker(this);
		}

		public void MoveObjectHere(UniversalObjectPhysics obj, AddressableAudioSource sound = null)
		{
			obj.SetMatrix(Register.Matrix);
			obj.SetTransform(
				Register.LocalPosition == badPosition ? gameObject.transform.position : Register.LocalPosition, false);
			if(sound != null) _ = SoundManager.PlayAtPosition(sound, Register.WorldPosition);
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var count = CountLocationMarkersWithSameID();
			Handles.Label(transform.position + Vector3.up * 2, $"id:{LocationID} - n:{count}");
		}

		private int CountLocationMarkersWithSameID()
		{
			var markers = FindObjectsByType<LocationMarker>(FindObjectsSortMode.None);
			return markers.Count(marker => marker.LocationID == LocationID);
		}

		[CustomEditor(typeof(LocationMarker))]
		public class LocationMarkerEditor : Editor
		{
			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();
				var script = (LocationMarker)target;
				EditorGUILayout.HelpBox(
					$"There are {script.CountLocationMarkersWithSameID()} location markers with ID {script.LocationID} detected in this scene.\n" +
					$"One of them will be choosen randomly when attempting to move to this ID.", MessageType.Info);
			}
		}
#endif
	}
}