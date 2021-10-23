using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Explosions;
using AddressableReferences;
using Managers;
using Strings;

namespace InGameEvents
{
	public class EventAsteroids : EventScriptBase
	{
		private MatrixInfo stationMatrix;

		private Queue<Vector3> impactCoords = new Queue<Vector3>();

		[SerializeField]
		private AsteroidEventLevels asteroidEventLevels = AsteroidEventLevels.Normal;

		[SerializeField]
		private int minMeteorAmount = 2;

		[SerializeField]
		private int maxMeteorAmount = 10;

		[SerializeField]
		private int minStrength = 100;

		[SerializeField]
		private int maxStrength = 500;

		[SerializeField]
		private int minTimeBetweenMeteors = 1;

		[SerializeField]
		private int maxTimeBetweenMeteors = 10;

		private bool IsMatrixInvalid()
		{
			if (stationMatrix != null) return false;

			Logger.LogError($"Unable to start \"{nameof(EventAsteroids)}\". Main station may not be initialized yet.", Category.Event);
			return true;
		}

		public override void OnEventStart()
		{
			stationMatrix = MatrixManager.MainStationMatrix;

			if (IsMatrixInvalid()) return;

			if (AnnounceEvent)
			{
				var text = "Proximity Alert:\nInbound Meteors have been detected.\nBrace for impact!";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);

				_ = SoundManager.PlayNetworked(CommonSounds.Instance.MeteorsAnnouncement);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			if (IsMatrixInvalid()) return;

			int asteroidAmount = UnityEngine.Random.Range(minMeteorAmount, maxMeteorAmount);

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

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}

		private IEnumerator SpawnMeteorsWithDelay(float asteroidAmount)
		{
			if (IsMatrixInvalid()) yield break;

			for (var i = 1; i <= asteroidAmount; i++)
			{
				int multiplier = 1;

				switch (asteroidEventLevels)
				{
					case AsteroidEventLevels.Normal:
						multiplier = 1;
						break;
					case AsteroidEventLevels.Threatening:
						multiplier = 2;
						break;
					case AsteroidEventLevels.Catastrophic:
						multiplier = 3;
						break;
				}

				var strength = UnityEngine.Random.Range(minStrength * multiplier, maxStrength * multiplier);

				Explosion.StartExplosion(impactCoords.Dequeue().ToLocalInt(stationMatrix), strength,
					stationMatrix.Matrix);

				yield return new WaitForSeconds(UnityEngine.Random.Range(minTimeBetweenMeteors, maxTimeBetweenMeteors));
			}

			base.OnEventStartTimed();
		}
	}

	public enum AsteroidEventLevels
	{
		Normal,
		Threatening,
		Catastrophic
	}
}
