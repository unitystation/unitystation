using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class DeviceRenamerMessage : ClientMessage<DeviceRenamerMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint ObjectID;
		public string NewName;
		public DeviceRenamer.RenameType RenameType;
	}

	public static void Send(GameObject Object, string name,DeviceRenamer.RenameType RenameType )
	{
		NetMessage msg = new NetMessage
		{
			ObjectID = Object == null ? NetId.Empty : Object.NetId(),
			NewName = name,
			RenameType = RenameType

		};
		Send(msg);
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;


		if (msg.ObjectID !=  NetId.Empty &&  msg.ObjectID != NetId.Invalid)
		{
			switch (msg.RenameType)
			{

				case DeviceRenamer.RenameType.ObjectName:
					msg.ObjectID.NetIdToGameObject().name = msg.NewName;
					break;
				case DeviceRenamer.RenameType.AttributeRename:
					var Attributes = msg.ObjectID.NetIdToGameObject().GetComponent<Attributes>();
					if (Attributes != null)
					{
						Attributes.SetInitialName(msg.NewName);
						Attributes.ServerSetArticleName(msg.NewName);
					}
					break;
				case DeviceRenamer.RenameType.MindRename:
					msg.ObjectID.NetIdToGameObject()?.Player()?.Mind?.SetPermanentName(msg.NewName);
					if (msg.ObjectID.NetIdToGameObject()?.Player()?.Script != null)
					{
						msg.ObjectID.NetIdToGameObject().Player().Script.playerName = msg.NewName;
					}
					break;
			}


		}

	}

}
