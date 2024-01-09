using System.Collections.Generic;
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
		public struct NetMessage : NetworkMessage
		{
			public string Path;
			public int OnlineStartPath;
			public string Newvalue;
			public string ValueName;
			public string MonoBehaviourName;
			public uint GameObject;
			public bool IsInvokeFunction;
			public PathMethod PathMethod;
		}

		public enum PathMethod
		{
			ID,
			ID_SubPath,
			MatrixID_SubPath,
			OnlineScene_SubPath
		}

		public override void Process(NetMessage msg)
		{

			if (CustomNetworkManager.Instance._isServer) return;
			GameObject NetworkedObject = null;
			switch (msg.PathMethod)
			{
				case PathMethod.ID:
					NetworkedObject = msg.GameObject.NetIdToGameObject();
					break;
				case PathMethod.ID_SubPath:
					NetworkedObject = TraversePath(msg.GameObject.NetIdToGameObject(), msg.Path);
					break;
				case PathMethod.MatrixID_SubPath:
					NetworkedObject = TraversePath(msg.GameObject.NetIdToGameObject().GetComponent<MatrixSync>().NetworkedMatrix.gameObject, msg.Path);
					break;
				case PathMethod.OnlineScene_SubPath:
					NetworkedObject = TraversePath(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[msg.OnlineStartPath], msg.Path);
					break;
			}
			AllowedReflection.ChangeVariableClient(NetworkedObject, msg.MonoBehaviourName, msg.ValueName,
				msg.Newvalue, msg.IsInvokeFunction);
		}


		public GameObject TraversePath(GameObject GameObject, string Path)
		{
			var PathSteps = JsonConvert.DeserializeObject<List<int>>(Path);
			PathSteps.Reverse();
			var SteppingThrough = GameObject.transform;
			foreach (var Step in PathSteps)
			{
				SteppingThrough = SteppingThrough.transform.GetChild(Step);
			}

			return SteppingThrough.gameObject;
		}

		public static NetMessage Send(string InNewvalue, string InValueName, string InMonoBehaviourName,
			GameObject InObject, bool IsInvokeFunction)
		{

			NetMessage msg = new NetMessage()
			{
				Newvalue = InNewvalue,
				ValueName = InValueName,
				MonoBehaviourName = InMonoBehaviourName,
				IsInvokeFunction = IsInvokeFunction
			};
			GetPathForMessage(ref msg, InObject);
			SendToAll(msg, 3);
			return msg;
		}


		public static void GetPathForMessage(ref NetMessage Message, GameObject InObject)
		{
			//Network object itself
			Message.GameObject = InObject.NetId();
			Message.PathMethod = PathMethod.ID;
			if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return;

			//Sprites on networked object/Sub- objects on network object
			List<int> Path = new List<int>();
			var ExploringObject = InObject.transform;
			Path.Clear();
			while (ExploringObject.parent != null)
			{
				Path.Add(ExploringObject.GetSiblingIndex());
				ExploringObject = ExploringObject.transform.parent;
				var Net = ExploringObject.GetComponent<NetworkIdentity>();
				if (Net != null)
				{
					Message.PathMethod = PathMethod.ID_SubPath;
					Message.GameObject = Net.netId;
					break;
				}
			}
			Message.Path = JsonConvert.SerializeObject(Path);

			if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return;
			//Tile map layers and above networking

			ExploringObject = InObject.transform;

			Path.Clear();
			Message.Path = "";
			while (ExploringObject.parent != null)
			{
				Path.Add(ExploringObject.GetSiblingIndex());
				ExploringObject = ExploringObject.transform.parent;
				var Net = ExploringObject.GetComponent<NetworkedMatrix>();
				if (Net != null)
				{
					Message.Path = JsonConvert.SerializeObject(Path);
					Message.PathMethod = PathMethod.MatrixID_SubPath;
					Message.GameObject = Net.MatrixSync.netId;
					break;
				}
			}
			Message.Path = JsonConvert.SerializeObject(Path);

			if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return;
			//Online scene Networking
			ExploringObject = InObject.transform;
			Message.Path = "";
			Path.Clear();
			while (ExploringObject.parent != null)
			{
				Path.Add(ExploringObject.GetSiblingIndex());
				ExploringObject = ExploringObject.transform.parent;
			}
			Message.OnlineStartPath = ExploringObject.GetSiblingIndex();
			Message.Path = JsonConvert.SerializeObject(Path);
			Message.PathMethod = PathMethod.OnlineScene_SubPath;
		}
	}
}