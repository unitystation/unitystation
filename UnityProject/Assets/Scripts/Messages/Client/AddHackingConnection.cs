using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class AddHackingConnection : ClientMessage
{

	public uint Player;
	public uint HackableObject;
	public string JsonData;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] { Player, HackableObject });
		int[] connectionToAdd = JsonConvert.DeserializeObject<int[]>(JsonData);

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

	public static AddHackingConnection Send(GameObject player, GameObject hackObject, int[] connectionToAdd)
	{
		AddHackingConnection msg = new AddHackingConnection
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			JsonData = JsonConvert.SerializeObject(connectionToAdd),
		};
		msg.Send();
		return msg;
	}
}
