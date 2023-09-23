using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UI.Core.NetUI;
using Messages.Server;

namespace UI.Objects.Shuttles
{
	/// all server only
	public class RadarList : NetUIDynamicList
	{
		public int Range = 160;
		public MatrixMove Origin;

		[SerializeField] private GUI_ShuttleControl shuttleControl;

		private List<TrackData> ObjectsToTrack = new List<TrackData>();


		private struct TrackData
		{
			public int Radius;
			public MapIconType MapIconType;
			public GameObject TrackedObject;
		}


		public void RefreshTrackedPos(bool update = true)
		{
			Vector2 originPos = Origin.ServerState.Position;

			// Refreshing positions of every item
			var entryArray = Entries.ToArray();
			for (var i = 0; i < entryArray.Length; i++)
			{
				var item = entryArray[i] as RadarEntry;
				if (!item) continue;

				item.RefreshTrackedPos(originPos);
				// If item is out of range, stop showing it and place into "out of range" list
				if (item.Position == TransformState.HiddenPos || ProjectionMagnitude(item.Position) > Range)
				{
					MasterRemoveItem(item);
				}
			}
			// Check if any item in "out of range" list should be shown again
			foreach (var ObjectToTrack in ObjectsToTrack)
			{
				bool TrackAlready = false;

				foreach (var Entrie in Entries)
				{
					RadarEntry item = Entrie as RadarEntry;
					if (item.TrackedObject == ObjectToTrack.TrackedObject)
					{
						TrackAlready = true;
						break;
					}
				}

				if (TrackAlready) continue;

				// Tracked objects are in map coordinate system, they should be tracked according to the shuttle, not the map origin
				Vector2 positionRelativeToShuttle = ObjectToTrack.TrackedObject.transform.position.To2() - originPos;
				if (ObjectToTrack.TrackedObject.transform.position != TransformState.HiddenPos && ProjectionMagnitude(positionRelativeToShuttle) <= Range)
				{
					var OneNew = AddItem();
					RadarEntry item = OneNew as RadarEntry;

					item.TrackedObject = ObjectToTrack.TrackedObject;
					item.Type = ObjectToTrack.MapIconType;
					item.Radius = ObjectToTrack.Radius;

					shuttleControl.PlayRadarDetectionSound();
				}
			}

			if (update)
			{
				UpdatePeepers();
			}
		}

		/// For square radar. For round radar item.Position.magnitude check should suffice.
		public static float ProjectionMagnitude(Vector3 pos)
		{
			var projX = Vector3.Project(pos, Vector3.right).magnitude;
			var projY = Vector3.Project(pos, Vector3.up).magnitude;
			return projX >= projY ? projX : projY;
		}

		public bool AddItems(MapIconType type, List<GameObject> objects, int radius = -1)
		{
			var objectsLoop = objects.ToArray();

			foreach (var trackData in ObjectsToTrack)
			{
				foreach (var Trackobject in objectsLoop)
				{
					if (trackData.TrackedObject == Trackobject)
					{
						objects.Remove(Trackobject);
					}
				}
			}

			foreach (var gameObject in objectsLoop)
			{
				var Track_Data = new TrackData()
				{
					MapIconType = type,
					TrackedObject = gameObject,
					Radius = radius
				};
				ObjectsToTrack.Add(Track_Data);
			}



			//rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			RefreshTrackedPos();

			return true;
		}


		/// Send updates about just one tracked object (intended for waypoint pin)
		/// <param name="trackedObject"></param>
		public void UpdateExclusive(GameObject trackedObject)
		{
			RefreshTrackedPos(false);

			bool notFound = true;

			var entries = Entries;
			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i] as RadarEntry;
				if (!entry || entry.TrackedObject != trackedObject) continue;

				notFound = false;

				List<ElementValue> valuesToSend = new List<ElementValue>(10) { ElementValue };
				var entryElements = entry.Elements;
				for (var j = 0; j < entryElements.Length; j++)
				{
					var element = entryElements[j];
					valuesToSend.Add(element.ElementValue);
				}
				TabUpdateMessage.SendToPeepers(containedInTab.Provider, containedInTab.Type, TabAction.Update, valuesToSend.ToArray());
			}
			//if not found (being hidden etc), send just the list entry count so it would disappear for peepers, too
			if (notFound)
			{
				TabUpdateMessage.SendToPeepers(containedInTab.Provider, containedInTab.Type, TabAction.Update, new[] { ElementValue });
			}
		}

		//Don't apply any clientside ordering and just rely on whatever server provided
		protected override void RefreshPositions() { }
		protected override void SetProperPosition(DynamicEntry entry, int sortIndex = 0) { }
	}
}
