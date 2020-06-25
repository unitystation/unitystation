using Mirror;
using UnityEngine;

namespace Items.PDA
{
	public class NoteUpdateMessage : ServerMessage
	{
		public uint PDAToUpdate;
		public uint Recipient;
		public string Message;


		public override void Process()
		{
			LoadMultipleObjects(new uint[] {Recipient, PDAToUpdate});
			var notes = NetworkObjects[1].GetComponent<PDANotesNetworkHandler>();
			notes.NoteString = Message;
			ControlTabs.RefreshTabs();
		}

		/// <summary>
		/// Sends the new string to the gameobject
		/// </summary>
		public static NoteUpdateMessage Send(GameObject recipient, GameObject noteToUpdate, string message)
		{
			NoteUpdateMessage msg = new NoteUpdateMessage
			{
				Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				PDAToUpdate = noteToUpdate.GetComponent<NetworkIdentity>().netId,
				Message = message
			};
			msg.SendTo(recipient);
			return msg;
		}
	}
}