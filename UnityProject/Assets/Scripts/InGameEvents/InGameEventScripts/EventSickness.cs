using Assets.Scripts.Health.Sickness;
using InGameEvents;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.InGameEvents.InGameEventScripts
{
	public class EventSickness: EventScriptBase
	{
		public override void OnEventStart(string serializedEventParameters)
		{
			SicknessEventParameters sicknessEventParameters = JsonConvert.DeserializeObject<SicknessEventParameters>(serializedEventParameters);

			Sickness sickness = SicknessManager.Instance.Sicknesses[sicknessEventParameters.SicknessIndex];

			foreach (ConnectedPlayer player in PlayerList.Instance.AllPlayers.PickRandom(sicknessEventParameters.PlayerToInfect).ToList())
				player.Script.playerHealth.AddSickness(sickness);
		}
	}
}
