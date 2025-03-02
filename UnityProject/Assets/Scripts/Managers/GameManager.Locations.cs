using System.Collections.Generic;
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

		public void MoveToLocationMarker(int locationID, UniversalObjectPhysics objectPhysics)
		{
			List<LocationMarker> markers = GetLocationMarkersWithId(locationID);
			if (markers.Count == 0)
			{
				Loggy.Error("No location markers found with ID " + locationID);
				return;
			}
			markers.PickRandom().MoveObjectHere(objectPhysics);
		}
	}
}