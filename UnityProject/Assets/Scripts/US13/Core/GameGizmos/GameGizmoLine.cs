using UnityEngine;
using US13.Managers.UpdateManager;

namespace US13.Core.GameGizmos
{
	public class GameGizmoLine : GameGizmo
	{

		public LineRenderer Renderer;

		public GameObject TrackingFrom;
		public Vector3 From;

		public GameObject TrackingTo;
		public Vector3 To;
		private bool callbackRegistered = false;


		public void SetUp(GameObject InTrackingFrom, Vector3 InFrom,   GameObject InTrackingTo, Vector3 InTo, Color color, float LineThickness, float time = -1)
		{
			TrackingFrom = InTrackingFrom;
			From = InFrom;

			TrackingTo = InTrackingTo;
			To = InTo;
			Renderer.startColor = color;
			Renderer.endColor = color;

			Renderer.startWidth = LineThickness;
			Renderer.endWidth = LineThickness;
			SecondsToLive = time;

			RegisterUpdateCallbacks();

			if (TrackingFrom != null)
			{
				Renderer.SetPosition(0,  TrackingFrom.transform.TransformPoint( From));
			}
			else
			{
				Renderer.SetPosition(0, From);
			}

			if (TrackingTo != null)
			{
				Renderer.SetPosition(1,    (TrackingTo.transform.TransformPoint(To)));
			}
			else
			{
				Renderer.SetPosition(1, To);
			}
		}

		private void RegisterUpdateCallbacks()
		{
			if (callbackRegistered) return;
			if (TrackingFrom != null || TrackingTo != null)
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				callbackRegistered = true;
			}
		}

		private void UnRegisterUpdateCallbacks()
		{
			if (TrackingFrom != null || TrackingTo != null)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			}
			callbackRegistered = false;
		}

		public void OnEnable()
		{
			RegisterUpdateCallbacks();
		}

		public void OnDisable()
		{
			UnRegisterUpdateCallbacks();
		}

		public void UpdateMe()
		{
			if (TrackingFrom != null)
			{
				Renderer.SetPosition(0, TrackingFrom.transform.TransformPoint( From));
			}
			else
			{
				Renderer.SetPosition(0, From);
			}

			if (TrackingTo != null)
			{
				Renderer.SetPosition(1, (TrackingTo.transform.TransformPoint(To)));
			}
			else
			{
				Renderer.SetPosition(1, To);
			}
		}
	}
}
