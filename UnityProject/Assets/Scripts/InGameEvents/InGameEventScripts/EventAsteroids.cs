using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventAsteroids : EventScriptBase
	{
		private MatrixInfo stationMatrix;

		private Queue<Vector3> impactCoords = new Queue<Vector3>();

		[SerializeField]
		private GameObject explosionPrefab = null;

		[SerializeField]
		private int minMeteorAmount = 2;

		[SerializeField]
		private int maxMeteorAmount = 10;

		[SerializeField]
		private float minRadius = 8f;

		[SerializeField]
		private float maxRadius = 20f;

		[SerializeField]
		private int minDamage = 100;

		[SerializeField]
		private int maxDamage = 500;

		[SerializeField]
		private int minTimeBetweenMeteors = 1;

		[SerializeField]
		private int maxTimeBetweenMeteors = 10;

		public override void OnEventStart()
		{
			stationMatrix = MatrixManager.MainStationMatrix;

			if (AnnounceEvent)
			{
				var text = "Proximity Alert:\nInbound Meteors have been detected.\nBrace for impact!";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			int asteroidAmount = UnityEngine.Random.Range(minMeteorAmount, maxMeteorAmount);

			if (explosionPrefab == null) return;

			for (var i = 1; i <= asteroidAmount; i++)
			{
				Vector3 position = new Vector3(UnityEngine.Random.Range(stationMatrix.WorldBounds.xMin, stationMatrix.WorldBounds.xMax), UnityEngine.Random.Range(stationMatrix.WorldBounds.yMin, stationMatrix.WorldBounds.yMax), 0);
				impactCoords.Enqueue(position);
			}

			_ = StartCoroutine(SpawnMeteorsWithDelay(asteroidAmount));
		}

		public override void OnEventEnd()
		{
			if (AnnounceEvent)
			{
				var text = "Situation Update:\nNo more Meteors have been detected.";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}
		}

		private IEnumerator SpawnMeteorsWithDelay(float asteroidAmount)
		{
			for (var i = 1; i <= asteroidAmount; i++)
			{
				var explosionObject = Instantiate(explosionPrefab, position: impactCoords.Dequeue(), rotation: stationMatrix.ObjectParent.rotation, parent: stationMatrix.ObjectParent).GetComponent<Explosion>();

				var radius = UnityEngine.Random.Range(minRadius, maxRadius);
				var damage = UnityEngine.Random.Range(minDamage, maxDamage);

				explosionObject.SetExplosionData(radius: radius, damage: damage);

				explosionObject.Explode(stationMatrix.Matrix);

				yield return new WaitForSeconds(UnityEngine.Random.Range(minTimeBetweenMeteors, maxTimeBetweenMeteors));
			}

			base.OnEventStartTimed();
		}
	}
}