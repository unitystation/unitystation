using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//long name I know. This is for syncing new clients when they join to all of the tile changes
public class TileChangesNewClientSync : ServerMessage
{
	public static short MessageType = (short) MessageTypes.TileChangesNewClientSync;
	public string data;
	public NetworkInstanceId ManagerSubject;

	public override IEnumerator Process()
	{
		yield return WaitFor(ManagerSubject);
//		TileChangeManager tm = NetworkObject.GetComponent<TileChangeManager>();
//		tm.InitServerSync(data);
	}

	public static TileChangesNewClientSync Send(GameObject managerSubject, GameObject recipient, string jsondata)
	{
		TileChangesNewClientSync msg =
			new TileChangesNewClientSync
			{ManagerSubject = managerSubject.GetComponent<NetworkIdentity>().netId,
			data = jsondata
			};

		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[Sync Tile ChangeData]");
	}
}
