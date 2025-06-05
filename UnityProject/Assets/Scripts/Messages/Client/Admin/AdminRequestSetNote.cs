using System.IO;
using Core.Admin.Logs;
using Messages.Client;
using Mirror;
using SecureStuff;
using UnityEngine;

public class AdminRequestSetNote : ClientMessage<AdminRequestSetNote.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string Notes;
		public string AccountID;
	}

	public override void Process(NetMessage msg)
	{
		if (HasPermission(TAG.ADMIN_INFO))
		{
			AdminLogsManager.AddNewLog( "Admin " , SentByPlayer?.GameObject,$" Set note for AccountID {msg.AccountID} to  log > {msg.Notes}", LogCategory.Admin);
			var NotePath = Path.Combine(AccessFile.AdminFolder, "Notes", msg.AccountID);
			AccessFile.WriteAllLines(NotePath, new string[]{msg.Notes}, FolderType.Logs, false);
		}
	}

	public static NetMessage Send(string Notes, string AccountID)
	{
		NetMessage msg = new()
		{
			Notes = Notes,
			AccountID = AccountID
		};

		Send(msg);
		return msg;
	}
}

