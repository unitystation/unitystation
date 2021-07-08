using System.Reflection;
using Mirror;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class UpdateClientValue : ServerMessage<UpdateClientValue.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Newvalue;
			public string ValueName;
			public string MonoBehaviourName;
			public uint GameObject;
		}

		public override void Process(NetMessage msg)
		{

			if (CustomNetworkManager.Instance._isServer) return;
			LoadNetworkObject(msg.GameObject);
			if (NetworkObject != null)
			{
				var workObject = NetworkObject.GetComponent(msg.MonoBehaviourName.Substring(msg.MonoBehaviourName.LastIndexOf('.') + 1));
				var Worktype = workObject.GetType();

				var infoField = Worktype.GetField(msg.ValueName);

				if (infoField != null)
				{
					infoField.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject, msg.Newvalue, infoField.FieldType));
					return;
				}

				var infoProperty = Worktype.GetProperty(msg.ValueName);
				if(infoProperty != null)
				{
					infoProperty.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject, msg.Newvalue, infoProperty.PropertyType));
					return;
				}
			}
		}

		public static NetMessage Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
			GameObject InObject)
		{
			uint netID = NetId.Empty;
			if (InObject != null)
			{
				netID = InObject.NetId();
			}
			NetMessage msg = new NetMessage()
			{
				Newvalue = InNewvalue,
				ValueName = InValueName,
				MonoBehaviourName = InMonoBehaviourName,
				GameObject = netID
			};

			SendToAll(msg, 3);
			return msg;
		}
	}
}