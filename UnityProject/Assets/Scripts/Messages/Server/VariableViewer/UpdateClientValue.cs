using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Logs;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecureStuff;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class UpdateClientValue : ServerMessage<UpdateClientValue.NetMessage>
	{
		//TODO NetworkedObject = TraversePath(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[msg.OnlineStartPath], msg.Path)

		public struct NetMessage : NetworkMessage
		{
			public string Newvalue;
			public string ValueName;
			public string MonoBehaviourName;
			public Modifying Modifying;
			public ClientObjectPath.PathData PathData;
		}


		public enum Modifying
		{
			ModifyingVariable,
			InvokingFunction,
			RenamingGameObject
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.Instance._isServer) return;
			var NetworkedObject = ClientObjectPath.GetObjectMessage(msg.PathData);

			if (msg.Modifying is UpdateClientValue.Modifying.InvokingFunction or UpdateClientValue.Modifying.ModifyingVariable)
			{
				AllowedReflection.ChangeVariableClient(NetworkedObject, msg.MonoBehaviourName, msg.ValueName,
					msg.Newvalue, msg.Modifying == UpdateClientValue.Modifying.InvokingFunction);
			}
			else if (msg.Modifying is UpdateClientValue.Modifying.RenamingGameObject)
			{
				NetworkIdentity NetID = null;
				var Handler = NetworkedObject.GetComponent<SpriteHandler>();
				if (Handler != null && Handler.NetworkThis)
				{
					NetID = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(NetworkedObject);
					SpriteHandlerManager.UnRegisterHandler(NetID, Handler);
				}

				NetworkedObject.name = msg.Newvalue;

				if (Handler != null && Handler.NetworkThis)
				{
					SpriteHandlerManager.RegisterHandler(NetID, Handler);
				}
			}
		}



		public static NetMessage Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
			GameObject InObject, Modifying Modifying)
		{

			NetMessage msg = new NetMessage()
			{
				Newvalue = InNewvalue,
				ValueName = InValueName,
				MonoBehaviourName = InMonoBehaviourName,
				Modifying = Modifying
			};
			msg.PathData = ClientObjectPath.GetPathForMessage(InObject, msg.Modifying == Modifying.RenamingGameObject ? msg.ValueName  : null );

			SendToAll(msg, 3);
			return msg;
		}



	}
}