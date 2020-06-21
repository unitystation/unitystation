using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventImmovableRod : EventScriptBase
	{
		private MatrixInfo StationMatrix;

		private Queue<Vector2> ImpactCoords = new Queue<Vector2>();

		public GameObject ExplosionPrefab = null;

		[SerializeField]
		private float MinRadius = 4f;

		[SerializeField]
		private float MaxRadius = 8f;

		[SerializeField]
		private int MinDamage = 200;

		[SerializeField]
		private int MaxDamage = 500;

		[SerializeField]
		private int TimeBetweenExplosions = 1;

		public override void OnEventStart()
		{
			StationMatrix = MatrixManager.MainStationMatrix;

			if (AnnounceEvent)
			{
				var text = "What the fuck is going on?!";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			if (ExplosionPrefab == null) return;

			var MaxCoord = new Vector2() { x = StationMatrix.WorldBounds.xMax , y = StationMatrix.WorldBounds.yMax };

			var MinCoord = new Vector2() { x = StationMatrix.WorldBounds.xMin, y = StationMatrix.WorldBounds.yMin };

			var biggestDistancePosible = (int)Vector2.Distance(MaxCoord, MinCoord);

			var midPoint = new Vector2() {x = (MaxCoord.x + MinCoord.x)/2, y = (MaxCoord.y + MinCoord.y)/2 };

			//x = RCos(θ), y = RSin(θ)

			var angle = UnityEngine.Random.Range(0f, 1f) * Math.PI * 2;

			var newX = (biggestDistancePosible / 2) * Math.Cos(angle);

			var newY = (biggestDistancePosible / 2) * Math.Sin(angle);

			//Generate opposite coord

			Double secondNewX;
			Double secondNewY;

			if (angle > 180)
			{
				secondNewX = (biggestDistancePosible / 2) * Math.Cos(angle - Math.PI);

				secondNewY = (biggestDistancePosible / 2) * Math.Sin(angle - Math.PI);
			}
			else
			{
				secondNewX = (biggestDistancePosible / 2) * Math.Cos(angle + Math.PI);

				secondNewY = (biggestDistancePosible / 2) * Math.Sin(angle + Math.PI);
			}

			var beginning = new Vector2() { x = (float)newX, y = (float)newY } + midPoint;

			var target = new Vector2() { x = (float)secondNewX, y = (float)secondNewY } + midPoint;

			//Adds original vector
			ImpactCoords.Enqueue(new Vector3() { x = beginning.x, y = beginning.y, z = 0 });

			Vector2 nextCoord = beginning;

			var amountOfImpactsNeeded = (biggestDistancePosible / MinRadius);

			//Fills list of Vectors all along rods path
			for (int i = 0; i < amountOfImpactsNeeded; i++)
			{
				//Vector 50 distance apart from prev vector
				nextCoord = Vector2.MoveTowards(nextCoord, target, MinRadius);
				ImpactCoords.Enqueue(new Vector3() {x = nextCoord.x, y = nextCoord.y, z = 0 });
			}

			Logger.Log("impact amount "+ amountOfImpactsNeeded);

			_ = StartCoroutine(SpawnMeteorsWithDelay(amountOfImpactsNeeded));
		}

		public override void OnEventEnd()
		{
			if (AnnounceEvent)
			{
				var text = "Seriously, what the fuck was that?!";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}
		}

		private IEnumerator SpawnMeteorsWithDelay(float asteroidAmount)
		{
			for (var i = 1; i <= asteroidAmount; i++)
			{
				var explosionObject = Instantiate(ExplosionPrefab, position: ImpactCoords.Dequeue(), rotation: StationMatrix.ObjectParent.rotation, parent: StationMatrix.ObjectParent).GetComponent<Explosion>();

				var radius = UnityEngine.Random.Range(MinRadius, MaxRadius);
				var damage = UnityEngine.Random.Range(MinDamage, MaxDamage);

				explosionObject.SetExplosionData(radius: radius, damage: damage);

				explosionObject.Explode(StationMatrix.Matrix);

				yield return new WaitForSeconds(TimeBetweenExplosions);
			}

			base.OnEventStartTimed();
		}
	}
}