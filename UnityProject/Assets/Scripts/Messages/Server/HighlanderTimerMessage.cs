using InGameEvents;
using Mirror;
using UI;
using UnityEngine;

namespace Messages.Server
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
