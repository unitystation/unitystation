using Mirror;
using UnityEngine;

namespace Items.PDA
{
	public class NoteUpdateMessage : ServerMessage
	{
		public class NoteUpdateMessageNetMessage : NetworkMessage
		{
			public uint PDAToUpdate;
			public uint Recipient;
			public string Message;
		}

		public override void Process<T>(T msg)
		{
			var newMsg = msg as NoteUpdateMessageNetMessage;
			if(newMsg == null) return;

			LoadMultipleObjects(new uint[] {newMsg.Recipient, newMsg.PDAToUpdate});
			var notes = NetworkObjects[1].GetComponent<PDANotesNetworkHandler>();
			notes.NoteString = newMsg.Message;
			ControlTabs.RefreshTabs();
		}

		/// <summary>
		/// Sends the new string to the gameobject
		/// </summary>
		public static NoteUpdateMessageNetMessage Send(GameObject recipient, GameObject noteToUpdate, string message)
		{
			NoteUpdateMessageNetMessage msg = new NoteUpdateMessageNetMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				PDAToUpdate = noteToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};

			new NoteUpdateMessage().SendTo(recipient, msg);
			return msg;
		}
	}
}