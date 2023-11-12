using System.Reflection;
using System.Transactions;
using Mirror;
using SecureStuff;
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
			public bool IsInvokeFunction;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.Instance._isServer) return;
			LoadNetworkObject(msg.GameObject);
			if (NetworkObject != null)
			{
				AllowedReflection.ChangeVariableClient(NetworkObject, msg.MonoBehaviourName, msg.ValueName, msg.Newvalue, msg.IsInvokeFunction);
			}
		}

		public static NetMessage Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
			GameObject InObject, bool IsInvokeFunction)
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
				GameObject = netID,
				IsInvokeFunction = IsInvokeFunction
			};

			SendToAll(msg, 3);
			return msg;
		}
	}
}