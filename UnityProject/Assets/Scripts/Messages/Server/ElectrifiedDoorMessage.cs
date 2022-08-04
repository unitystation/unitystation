using System.Linq;
using Doors;
using Doors.Modules;
using Mirror;
using Player;

namespace Messages.Server
{
	public class ElectrifiedDoorMessage : ServerMessage<ElectrifiedDoorMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint NetId;
			public bool State;
		}

		public override void Process(NetMessage msg)
		{
			//Client
			LoadNetworkObject(msg.NetId);
			if(NetworkObject == null) return;
			var electrifiedDoor = NetworkObject.GetComponentInChildren<ElectrifiedDoorModule>();
			if(electrifiedDoor == null) return;

			electrifiedDoor.NewSpriteState(msg.State);
		}

		public static void Send(DoorMasterController electrifiedDoor, bool state)
		{
			//Send to all who have ElectrifiedDoorViewer
			var players = PlayerList.Instance.InGamePlayers.Where(x =>
				x.Script.GetComponent<ElectrifiedDoorViewer>() != null);

			var msg = new NetMessage
			{
				NetId = electrifiedDoor.netId,
				State = state
			};

			foreach (var player in players)
			{
				SendTo(player, msg);
			}
		}

		public static void SendTo(NetworkConnectionToClient conn, DoorMasterController electrifiedDoor, bool state)
		{
			var msg = new NetMessage
			{
				NetId = electrifiedDoor.netId,
				State = state
			};

			SendTo(conn, msg);
		}
	}
}