using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElectricalStatsMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ElectricalStatsMessage;
	public string JsonData;
	public NetworkInstanceId Recipient;//fixme: Recipient is redundant! Can be safely removed

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);

		ElectronicData data = JsonUtility.FromJson<ElectronicData>(JsonData);

		string newChatText = "";
		newChatText += $"Current: {data.CurrentInWire} \n";
		newChatText += $"Voltage: {data.ActualVoltage} \n";
		newChatText += $"Resistance: {data.EstimatedResistance}";
		
		ChatRelay.Instance.AddToChatLogClient(newChatText, ChatChannel.Examine);
	}

	public static ElectricalStatsMessage  Send(GameObject recipient, string data)
	{
		ElectricalStatsMessage  msg =
			new ElectricalStatsMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		msg.SendTo(recipient);
		return msg;
	}
}
