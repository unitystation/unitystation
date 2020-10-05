using System;
using Mirror;
using UnityEngine;

namespace Items.PDA
{
	public class PDANotesNetworkHandler : NetworkBehaviour
	{

		private string serverString;

		public string NoteString { get; set; } = "";

		[Server]
		public void SetServerString(string msg)
		{
			serverString = msg;
		}
		[Server]
		public void UpdatePlayer(GameObject recipient)
		{
			NoteUpdateMessage.Send(recipient, gameObject, serverString);
		}
	}
}
