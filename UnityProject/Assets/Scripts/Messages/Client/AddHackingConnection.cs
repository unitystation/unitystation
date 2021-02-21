using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class AddHackingConnection : ClientMessage
{
	public struct AddHackingConnectionNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
		public string JsonData;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AddHackingConnectionNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AddHackingConnectionNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

		LoadMultipleObjects(new uint[] { newMsg.Player, newMsg.HackableObject });
		int[] connectionToAdd = JsonConvert.DeserializeObject<int[]>(newMsg.JsonData);

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
		if (hackingProcess.ServerPlayerCanAddConnection(playerScript, connectionToAdd))
		{
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.WireMend, playerScript.WorldPos);
			hackingProcess.AddNodeConnection(connectionToAdd);
			HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
		}
	}

	public static AddHackingConnectionNetMessage Send(GameObject player, GameObject hackObject, int[] connectionToAdd)
	{
		AddHackingConnectionNetMessage msg = new AddHackingConnectionNetMessage
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			JsonData = JsonConvert.SerializeObject(connectionToAdd),
		};

		new AddHackingConnection().Send(msg);
		return msg;
	}
}
