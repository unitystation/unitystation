using Messages.Server;
using Mirror;
using UnityEngine;
using UI;

namespace Items.PDA
{
	public class NoteUpdateMessage : ServerMessage<NoteUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint PDAToUpdate;
			public uint Recipient;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] {msg.Recipient, msg.PDAToUpdate});
			var notes = NetworkObjects[1].GetComponent<PDANotesNetworkHandler>();
			notes.NoteString = msg.Message;
			ControlTabs.RefreshTabs();
		}

		/// <summary>
		/// Sends the new string to the gameobject
		/// </summary>
		public static NetMessage Send(GameObject recipient, GameObject noteToUpdate, string message)
		{
			NetMessage msg = new NetMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				PDAToUpdate = noteToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
