using Health.Sickness;
using Newtonsoft.Json;
using System.Linq;

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
				var text = "Incoming Public Health Report:\nSome people on the station are afflicted by some disease.";

				CentComm.MakeAnnouncement(CentComm.CentCommAnnounceTemplate, text, CentComm.UpdateSound.alert);
			}
		}

		private static void SpawnSickness(string serializedEventParameters)
		{
			SicknessEventParameters sicknessEventParameters = JsonConvert.DeserializeObject<SicknessEventParameters>(serializedEventParameters);

			Sickness sickness = SicknessManager.Instance.Sicknesses[sicknessEventParameters.SicknessIndex];

			foreach (ConnectedPlayer player in PlayerList.Instance.AllPlayers.PickRandom(sicknessEventParameters.PlayerToInfect).ToList())
			{
				if ((player.Script != null) && (player.Script.playerHealth != null))
					player.Script.playerHealth.AddSickness(sickness);
			}
		}
	}
}
