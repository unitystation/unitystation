using System.Reflection;
using Mirror;
using UnityEngine;

public class UpdateClientValue : ServerMessage
{
	public class UpdateClientValueNetMessage : NetworkMessage
	{
		public string Newvalue;
		public string ValueName;
		public string MonoBehaviourName;
		public uint GameObject;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as UpdateClientValueNetMessage;
		if(newMsg == null) return;

		if (CustomNetworkManager.Instance._isServer) return;
		LoadNetworkObject(newMsg.GameObject);
		if (NetworkObject != null)
		{
			var workObject = NetworkObject.GetComponent(newMsg.MonoBehaviourName.Substring(newMsg.MonoBehaviourName.LastIndexOf('.') + 1));
			var Worktype = workObject.GetType();

			var infoField = Worktype.GetField(newMsg.ValueName);

			if (infoField != null)
			{
				infoField.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject, newMsg.Newvalue, infoField.FieldType));
				return;
			}

			var infoProperty = Worktype.GetProperty(newMsg.ValueName);
			if(infoProperty != null)
			{
				infoProperty.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject, newMsg.Newvalue, infoProperty.PropertyType));
				return;
			}
		}
	}

	public static UpdateClientValueNetMessage Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
		GameObject InObject)
	{
		uint netID = NetId.Empty;
		if (InObject != null)
		{
			netID = InObject.NetId();
		}
		UpdateClientValueNetMessage msg = new UpdateClientValueNetMessage()
		{
			Newvalue = InNewvalue,
			ValueName = InValueName,
			MonoBehaviourName = InMonoBehaviourName,
			GameObject = netID
		};
		new UpdateClientValue().SendToAll(msg);
		return msg;
	}
}