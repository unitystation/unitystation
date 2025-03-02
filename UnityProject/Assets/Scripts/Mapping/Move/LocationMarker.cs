using System;
using System.Linq;
using Core.Physics;
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

		public void MoveObjectHere(UniversalObjectPhysics obj)
		{
			obj.SetTransform(Register.LocalPosition, false);
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			var count = CountLocationMarkersWithSameID();
			if (count == 0) return;
			Handles.Label(transform.position + Vector3.up * 2,
				$"There are{count} location markers with this ID" +
				$"\n They will be choosen randomly when attempting to use this ID.");
		}

		private int CountLocationMarkersWithSameID()
		{
			var markers = FindObjectsByType<LocationMarker>(FindObjectsSortMode.None);
			return markers.Count(marker => marker.LocationID == LocationID);
		}
#endif
	}
}