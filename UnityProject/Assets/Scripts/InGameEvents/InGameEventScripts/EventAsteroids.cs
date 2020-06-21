using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventAsteroids : EventScriptBase
	{
		private MatrixInfo StationMatrix;

		private Queue<Vector3> ImpactCoords = new Queue<Vector3>();

		public GameObject ExplosionPrefab = null;

		private void Start()
		{
			InGameEventsManager.Instance.AddEventToList(this);
		}

		public override void OnEventStart()
		{
			StationMatrix = MatrixManager.MainStationMatrix;

			var text = "Proximity Alert:\nInbound Asteroids have been detected.\nBrace for impact!";

			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			int asteroidAmount = UnityEngine.Random.Range(2, 10);

			for (var i = 1; i <= asteroidAmount; i++)
			{
				Vector3 position = new Vector3(UnityEngine.Random.Range(StationMatrix.WorldBounds.xMin, StationMatrix.WorldBounds.xMax), UnityEngine.Random.Range(StationMatrix.WorldBounds.yMin, StationMatrix.WorldBounds.yMax), 0);
				ImpactCoords.Enqueue(position);
			}

			var queueCount = ImpactCoords.Count;

			if (queueCount == 0 || ExplosionPrefab == null) return;

			_ = StartCoroutine(WaitTime(queueCount));
		}

		public override void OnEventEnd()
		{
			var text = "Situtation Update:\nNo more asteroids have been detected.";

			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
		}

		private IEnumerator WaitTime(float queueCount)
		{
			for (var i = 1; i <= queueCount; i++)
			{
				var explosionObject = Instantiate(ExplosionPrefab, position: ImpactCoords.Dequeue(), rotation: StationMatrix.ObjectParent.rotation, parent: StationMatrix.ObjectParent).GetComponent<Explosion>();

				var radius = UnityEngine.Random.Range(8f, 20f);
				var damage = UnityEngine.Random.Range(100, 500);

				explosionObject.SetExplosionData(radius: radius, damage: damage);

				explosionObject.Explode(StationMatrix.Matrix);

				yield return new WaitForSeconds(UnityEngine.Random.Range(1, 5));
			}

			base.OnEventStartTimed();
		}
	}
}