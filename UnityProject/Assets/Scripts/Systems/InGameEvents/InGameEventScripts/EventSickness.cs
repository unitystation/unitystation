using Newtonsoft.Json;
using System.Linq;
using Chemistry;
using HealthV2.Sickness;
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
				var text = "Incoming Public Health Report:\nSome individuals onboard the station may have been afflicted by an unknown pathogen. " +
				           "Please remain clam and practice social distancing while visiting the station's virologist for sample collection.";

				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}

		private static void SpawnSickness(string serializedEventParameters)
		{
			//TODO: Reimplent sickness event.

			SicknessEventParameters sicknessEventParameters;

			//TODO: More players == more chance of deadlier disease?

			if (serializedEventParameters == null)
			{
				sicknessEventParameters = new SicknessEventParameters()
				{
					PlayerToInfect = Random.Range(1, Mathf.Max(1, PlayerList.Instance.AllPlayers.Count)),
					SicknessIndex = Random.Range(0, CureManager.Instance.CureableSicknesses.Count),
					Strength = Random.Range(0,2)
				};
			}
			else
			{
				sicknessEventParameters = JsonConvert.DeserializeObject<SicknessEventParameters>(serializedEventParameters);
			}

			CureManager.CureableSickness sickness = CureManager.Instance.CureableSicknesses[sicknessEventParameters.SicknessIndex];
			int infected = 0;
			foreach (PlayerInfo player in PlayerList.Instance.AllPlayers.PickRandom(sicknessEventParameters.PlayerToInfect).ToList())
			{
				if (player.Script is null || player.Script.playerHealth is null) continue;

				player.Script.playerHealth.reagentPoolSystem.BloodPool.Add(sickness.Sickness, sicknessEventParameters.Strength * 5);
				if (++infected >= sicknessEventParameters.PlayerToInfect) break;

			}
		}
	}
}
