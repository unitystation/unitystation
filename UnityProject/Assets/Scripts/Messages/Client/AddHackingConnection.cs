using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class AddHackingConnection : ClientMessage
{
	public class AddHackingConnectionNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
		public string JsonData;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as AddHackingConnectionNetMessage;
		if(newMsg == null) return;

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
