using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGameEvents
{
	public class EventImmovableRod : EventScriptBase
	{
		private MatrixInfo stationMatrix;

		private Queue<Vector3> impactCoords = new Queue<Vector3>();

		[SerializeField]
		private int minStrength = 200;

		[SerializeField]
		private int maxStrength = 500;

		[SerializeField]
		private float timeBetweenExplosions = 1f;

		private const int DISTANCE_BETWEEN_EXPLOSIONS = 8;

		public override void OnEventStart()
		{
			stationMatrix = MatrixManager.MainStationMatrix;

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
			var MaxCoord = new Vector2() { x = stationMatrix.WorldBounds.xMax , y = stationMatrix.WorldBounds.yMax };

			var MinCoord = new Vector2() { x = stationMatrix.WorldBounds.xMin, y = stationMatrix.WorldBounds.yMin };

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
			impactCoords.Enqueue(new Vector3() { x = beginning.x, y = beginning.y, z = 0 });

			Vector2 nextCoord = beginning;

			var amountOfImpactsNeeded = (biggestDistancePosible / DISTANCE_BETWEEN_EXPLOSIONS);

			//Fills list of Vectors all along rods path
			for (int i = 0; i < amountOfImpactsNeeded; i++)
			{
				//Vector 50 distance apart from prev vector
				nextCoord = Vector2.MoveTowards(nextCoord, target, DISTANCE_BETWEEN_EXPLOSIONS);
				impactCoords.Enqueue(new Vector3() {x = nextCoord.x, y = nextCoord.y, z = 0 });
			}

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
				var strength = UnityEngine.Random.Range(minStrength, maxStrength);

				Explosions.Explosion.StartExplosion(impactCoords.Dequeue().ToLocalInt(stationMatrix), strength,
					stationMatrix.Matrix);

				yield return new WaitForSeconds(timeBetweenExplosions);
			}

			base.OnEventStartTimed();
		}
	}
}