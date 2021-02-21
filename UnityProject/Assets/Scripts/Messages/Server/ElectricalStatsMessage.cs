using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

//TODO We need electrical stats to be sent to the PDA Power-ON Cartridge for engineering pdas only
//atm its just being sent to examine channel
public class ElectricalStatsMessage : ServerMessage
{
	public class ElectricalStatsMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public uint Recipient;//fixme: Recipient is redundant! Can be safely removed
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as ElectricalStatsMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Recipient);
		ElectronicData data = JsonUtility.FromJson<ElectronicData>(newMsg.JsonData);

		string newChatText = "";
		newChatText += $"Current: {data.CurrentInWire} \n";
		newChatText += $"Voltage: {data.ActualVoltage} \n";
		newChatText += $"Resistance: {data.EstimatedResistance}";
		Chat.AddExamineMsgToClient(newChatText);
	}

	public static ElectricalStatsMessageNetMessage  Send(GameObject recipient, string data)
	{
		ElectricalStatsMessageNetMessage  msg =
			new ElectricalStatsMessageNetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		new ElectricalStatsMessage().SendTo(recipient, msg);
		return msg;
	}
}
