using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Client
{
	public class AddHackingConnection : ClientMessage<AddHackingConnection.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
			public uint HackableObject;
			public string JsonData;
		}

		public override void Process(NetMessage msg)
		{
			// LoadMultipleObjects(new uint[] { msg.Player, msg.HackableObject });
			// int[] connectionToAdd = JsonConvert.DeserializeObject<int[]>(msg.JsonData);
			//
			// var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
			// var hackObject = NetworkObjects[1];
			// HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
			// if (hackingProcess.ServerPlayerCanAddConnection(playerScript, connectionToAdd))
			// {
			// 	SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.WireMend, playerScript.WorldPos);
			// 	hackingProcess.AddNodeConnection(connectionToAdd);
			// 	HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
			// }
		}

		public static NetMessage Send(GameObject player, GameObject hackObject, int[] connectionToAdd)
		{
			NetMessage msg = new NetMessage
			{
				Player = player.GetComponent<NetworkIdentity>().netId,
				HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
				JsonData = JsonConvert.SerializeObject(connectionToAdd),
			};

			Send(msg);
			return msg;
		}
	}
}
