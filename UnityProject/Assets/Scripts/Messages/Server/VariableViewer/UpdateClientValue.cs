using System.Reflection;
using UnityEngine;

public class UpdateClientValue : ServerMessage
{
	public string Newvalue;
	public string ValueName;
	public string MonoBehaviourName;
	public uint GameObject;

	public override void Process()
	{
		if (CustomNetworkManager.Instance._isServer) return;
		LoadNetworkObject(GameObject);
		if (NetworkObject != null)
		{
			var workObject = NetworkObject.GetComponent(MonoBehaviourName.Substring(MonoBehaviourName.LastIndexOf('.') + 1));
			var Worktype = workObject.GetType();

			var infoField = Worktype.GetField(ValueName);

			if (infoField != null)
			{
				infoField.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject,Newvalue, infoField.FieldType));
				return;
			}

			var infoProperty = Worktype.GetProperty(ValueName);
			if(infoProperty != null)
			{
				infoProperty.SetValue(workObject,  Librarian.Page.DeSerialiseValue(workObject,Newvalue, infoProperty.PropertyType));
				return;
			}
		}
	}

	public static UpdateClientValue Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
		GameObject InObject)
	{
		UpdateClientValue msg = new UpdateClientValue()
		{
			Newvalue = InNewvalue,
			ValueName = InValueName,
			MonoBehaviourName = InMonoBehaviourName,
			GameObject = InObject.NetId()
		};
		msg.SendToAll();
		return msg;
	}
}