using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core.Physics;
using Logs;
using Mapping.Move;

namespace Managers
{
	public partial class GameManager
	{
		public List<LocationMarker> LocationMarkers = new List<LocationMarker>();

		public List<LocationMarker> GetLocationMarkersWithId(int locationID)
		{
			List<LocationMarker> markers = new List<LocationMarker>();
			foreach (LocationMarker marker in LocationMarkers)
			{
				if (marker.LocationID == locationID)
				{
					markers.Add(marker);
				}
			}
			return markers;
		}

		public void AddLocationMarker(LocationMarker marker)
		{
			LocationMarkers.Add(marker);
		}

		public void RemoveLocationMarker(LocationMarker marker)
		{
			LocationMarkers.Remove(marker);
		}

		public void MoveToLocationMarker(int locationID, UniversalObjectPhysics objectPhysics, bool skipOccupied = true, AddressableAudioSource sound = null)
		{
			List<LocationMarker> markers = GetLocationMarkersWithId(locationID);
			if (markers.Count == 0)
			{
				Loggy.Error("No location markers found with ID " + locationID);
				return;
			}
			if (skipOccupied)
			{
				markers.PickRandom().MoveObjectHere(objectPhysics, sound);
			}
			else
			{
				SearchForUnoccupiedLocationMarkers(markers, objectPhysics, sound);
			}
		}

		private void SearchForUnoccupiedLocationMarkers(List<LocationMarker> markers, UniversalObjectPhysics objectPhysics, AddressableAudioSource sound)
		{
			if (markers.Count == 0) return;
			foreach (LocationMarker marker in markers)
			{
				// linq bad. we're counting on this happening on the start of the round before players do anything.
				if (marker.Register.Matrix
					    .Get<UniversalObjectPhysics>(marker.Register.LocalPosition, CustomNetworkManager.IsServer).Any() == false)
				{
					marker.MoveObjectHere(objectPhysics, sound: sound);
					return;
				}
			}
			Loggy.Error("No unoccupied location markers found with ID " + markers[0].LocationID);
			markers.PickRandom().MoveObjectHere(objectPhysics, sound: sound);
		}
	}
}