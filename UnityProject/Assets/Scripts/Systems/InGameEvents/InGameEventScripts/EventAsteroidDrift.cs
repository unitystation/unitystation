using Managers;
using Map;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	public class EventAsteroidDrift : EventScriptBase
	{
		private MatrixInfo asteroid = null;
		private float velocity = 250;

		public override void OnEventStart()
		{
			asteroid =
				MatrixManager.Instance.ActiveMatricesList.FindAll(x => x.Name.ToLower().Contains("asteroid")).PickRandom();

			Debug.Log(asteroid == null);
			if (asteroid == null)
			{
				return;
			}
			Debug.Log(asteroid.Name);

			if (AnnounceEvent)
			{
				var text = "An asteroid has been detected drifting away from its position." +
				           "All shuttles are advised to practice caution and slow down when navigating space.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);
				_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementAlert);
			}

			if (FakeEvent) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			velocity = Random.Range(0.45f, 0.95f);
			UpdateManager.Add(CallbackType.UPDATE, MoveShip);
			base.OnEventStartTimed();
		}

		public override void OnEventEndTimed()
		{
			UpdateManager.Remove(CallbackType.UPDATE, MoveShip);
			asteroid.MatrixMove.NetworkedMatrixMove.WorldCurrentVelocity = Vector3.zero;
			var text = "The asteroid has appeared to slow down. Last known location is at: " +
			           asteroid.GameObject.transform.position.CutToInt().ToString();

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.NoSound);
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.AnnouncementAlert);
			base.OnEventEndTimed();
		}

		private void MoveShip()
		{
			if (asteroid.GameObject.transform.position.y.Approx(0) ||
			    asteroid.GameObject.transform.position.x.Approx(0))
			{
				OnEventEndTimed();
				return;
			}
			var mainstation = MatrixManager.MainStationMatrix;
			Vector3 direction = asteroid.GameObject.transform.position - mainstation.GameObject.transform.position;
			direction.Normalize();
			direction *= -velocity;
			asteroid.MatrixMove.NetworkedMatrixMove.WorldCurrentVelocity += direction;
		}
	}
}
