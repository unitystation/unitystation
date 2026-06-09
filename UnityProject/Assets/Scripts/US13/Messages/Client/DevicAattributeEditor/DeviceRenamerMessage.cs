using Mirror;
using UnityEngine;
using US13.Items;
using US13.Messages.Client.Admin;
using US13.UI.Systems.AdminTools.DevTools;
using Util;

namespace US13.Messages.Client.DevicAattributeEditor
{
	public class DeviceRenamerMessage : ClientMessage<DeviceRenamerMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ObjectID;
			public string NewName;
			public DeviceAttributeEditor.RenameType RenameType;
		}

		public static void Send(GameObject Object, string name,DeviceAttributeEditor.RenameType RenameType )
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
			if (HasPermission(TAG.MAP_RENAME) == false) return;


			if (msg.ObjectID !=  NetId.Empty &&  msg.ObjectID != NetId.Invalid)
			{
				switch (msg.RenameType)
				{

					case DeviceAttributeEditor.RenameType.ObjectName:
						msg.ObjectID.NetIdToGameObject().name = msg.NewName;
						break;
					case DeviceAttributeEditor.RenameType.AttributeRename:
						var Attributes = msg.ObjectID.NetIdToGameObject().GetComponent<Attributes>();
						if (Attributes != null)
						{
							Attributes.SetInitialName(msg.NewName);
							Attributes.ServerSetArticleName(msg.NewName);
						}
						break;
					case DeviceAttributeEditor.RenameType.MindRename:
						msg.ObjectID.NetIdToGameObject()?.Player()?.Mind?.SetPermanentName(msg.NewName);
						if (msg.ObjectID.NetIdToGameObject()?.Player()?.Script != null)
						{
							msg.ObjectID.NetIdToGameObject().Player().Script.PlayerScriptVisible.playerName = msg.NewName;
						}
						break;
				}


			}

		}

	}
}
