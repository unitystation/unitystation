using Health.Sickness;
using Newtonsoft.Json;
using System.Linq;
using Managers;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	/// <summary>
	/// The sickness event
	/// </summary>
	public class EventSickness: EventScriptBase
	{
		public override void OnEventStart(string serializedEventParameters)
		{
			if (!FakeEvent)
			{
				SpawnSickness(serializedEventParameters);
			}

			base.OnEventStart(serializedEventParameters);
		}

		public override void OnEventEndTimed()
		{
			if (AnnounceEvent)
			{
				var text = "Incoming Public Health Report:\nSome individuals onboard the station may have been afflicted by an unknown disease. " +
				           "Please remain clam and practice social distancing while visiting the station's virologist for sample collection.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}

		private static void SpawnSickness(string serializedEventParameters)
		{
			SicknessEventParameters sicknessEventParameters;

			//If null means its a server random event so has no default parameters
			//TODO: when infection system is more deadly change random effects based on player numbers
			//TODO: eg more players == more chance of deadlier disease?
			if (serializedEventParameters == null)
			{
				sicknessEventParameters = new SicknessEventParameters()
				{
					PlayerToInfect = Random.Range(1, Mathf.Max(1, PlayerList.Instance.AllPlayers.Count)),
					SicknessIndex = Random.Range(0, SicknessManager.Instance.Sicknesses.Count)
				};
			}
			else
			{
				sicknessEventParameters = JsonConvert.DeserializeObject<SicknessEventParameters>(serializedEventParameters);
			}

			Sickness sickness = SicknessManager.Instance.Sicknesses[sicknessEventParameters.SicknessIndex];
			SpawnResult spawnResult = Spawn.ServerPrefab(sickness.gameObject);
			if(spawnResult.Successful == false || spawnResult.GameObject.TryGetComponent<Sickness>(out var newSick) == false) return;

			foreach (PlayerInfo player in PlayerList.Instance.AllPlayers.PickRandom(sicknessEventParameters.PlayerToInfect).ToList())
			{
				if (player.Script != null && player.Script.playerHealth != null)
				{
					SpawnResult sicknessResult = Spawn.ServerPrefab(sickness.gameObject);
					sicknessResult.GameObject.GetComponent<Sickness>().SetCure(newSick.CureForSickness);
					player.Script.playerHealth.AddSickness(sicknessResult.GameObject.GetComponent<Sickness>());
				}
			}
		}
	}
}
