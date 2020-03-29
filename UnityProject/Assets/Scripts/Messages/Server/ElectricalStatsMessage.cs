using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

//TODO We need electrical stats to be sent to the PDA Power-ON Cartridge for engineering pdas only
//atm its just being sent to examine channel
public class ElectricalStatsMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.ElectricalStatsMessage;
	public string JsonData;
	public uint Recipient;//fixme: Recipient is redundant! Can be safely removed

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		ElectronicData data = JsonUtility.FromJson<ElectronicData>(JsonData);

		string newChatText = "";
		newChatText += $"Current: {data.CurrentInWire} \n";
		newChatText += $"Voltage: {data.ActualVoltage} \n";
		newChatText += $"Resistance: {data.EstimatedResistance}";
		Chat.AddExamineMsgToClient(newChatText);
	}

	public static ElectricalStatsMessage  Send(GameObject recipient, string data)
	{
		ElectricalStatsMessage  msg =
			new ElectricalStatsMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		msg.SendTo(recipient);
		return msg;
	}
}
