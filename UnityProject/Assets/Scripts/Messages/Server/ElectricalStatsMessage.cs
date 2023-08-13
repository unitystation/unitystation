using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Systems.Electricity;

namespace Messages.Server
{
	// TODO We need electrical stats to be sent to the PDA Power-ON Cartridge for engineering pdas only
	// atm its just being sent to examine channel
	public class ElectricalStatsMessage : ServerMessage<ElectricalStatsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient; // TODO FIXME: Recipient is redundant! Can be safely removed
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			ElectronicData data = JsonConvert.DeserializeObject<ElectronicData>(msg.JsonData);

			string newChatText = "";
			newChatText += $"Current: {data.CurrentInWire} \n";
			newChatText += $"Voltage: {data.ActualVoltage} \n";
			newChatText += $"Resistance: {data.EstimatedResistance}";
			Chat.AddExamineMsgToClient(newChatText);
		}

		public static NetMessage  Send(GameObject recipient, string data)
		{
			NetMessage  msg =
				new NetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
