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

		[SerializeField]
		private int MinMeteorAmount = 2;

		[SerializeField]
		private int MaxMeteorAmount = 10;

		[SerializeField]
		private float MinRadius = 8f;

		[SerializeField]
		private float MaxRadius = 20f;

		[SerializeField]
		private int MinDamage = 100;

		[SerializeField]
		private int MaxDamage = 500;

		private void Start()
		{
			InGameEventsManager.Instance.AddEventToList(this);
		}

		public override void OnEventStart()
		{
			StationMatrix = MatrixManager.MainStationMatrix;

			var text = "Proximity Alert:\nInbound Meteors have been detected.\nBrace for impact!";

			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			int asteroidAmount = UnityEngine.Random.Range(MinMeteorAmount, MaxMeteorAmount);

			for (var i = 1; i <= asteroidAmount; i++)
			{
				Vector3 position = new Vector3(UnityEngine.Random.Range(StationMatrix.WorldBounds.xMin, StationMatrix.WorldBounds.xMax), UnityEngine.Random.Range(StationMatrix.WorldBounds.yMin, StationMatrix.WorldBounds.yMax), 0);
				ImpactCoords.Enqueue(position);
			}

			var queueCount = ImpactCoords.Count;

			if (queueCount == 0 || ExplosionPrefab == null) return;

			_ = StartCoroutine(SpawnMeteorsWithDelay(queueCount));
		}

		public override void OnEventEnd()
		{
			var text = "Situation Update:\nNo more Meteors have been detected.";

			CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
		}

		private IEnumerator SpawnMeteorsWithDelay(float queueCount)
		{
			for (var i = 1; i <= queueCount; i++)
			{
				var explosionObject = Instantiate(ExplosionPrefab, position: ImpactCoords.Dequeue(), rotation: StationMatrix.ObjectParent.rotation, parent: StationMatrix.ObjectParent).GetComponent<Explosion>();

				var radius = UnityEngine.Random.Range(MinRadius, MaxRadius);
				var damage = UnityEngine.Random.Range(MinDamage, MaxDamage);

				explosionObject.SetExplosionData(radius: radius, damage: damage);

				explosionObject.Explode(StationMatrix.Matrix);

				yield return new WaitForSeconds(UnityEngine.Random.Range(1, 5));
			}

			base.OnEventStartTimed();
		}
	}
}