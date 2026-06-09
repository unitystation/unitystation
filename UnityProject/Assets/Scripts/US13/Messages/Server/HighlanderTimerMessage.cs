using Mirror;
using US13.Managers;
using US13.Systems.InGameEvents;
using US13.Systems.InGameEvents.InGameEventScripts;
using US13.UI.Systems.MainHUD;

namespace US13.Messages.Server
{
	public class HighlanderTimerMessage : ServerMessage<HighlanderTimerMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public float Time;
		}

		public override void Process(NetMessage msg)
		{
			HighlanderTimerUI.Instance.Show(msg.Time);
		}

		public static void Send(PlayerInfo player)
		{
			var highlanderEvent =  InGameEventsManager.Instance.ListOfAntagonistEventScripts.Find(x => x.GetType() == typeof(EventHighlander))
				.GetComponent<EventHighlander>();
			if(highlanderEvent.RemainingTime <= 10) return;
			var msg = new NetMessage
			{
				Time = highlanderEvent.RemainingTime,
			};
			SendTo(player, msg);
		}
	}
}
