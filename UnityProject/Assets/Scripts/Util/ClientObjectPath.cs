using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages.Server.VariableViewer;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using Shuttles;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using Util;

public static class ClientObjectPath
{
	public enum PathMethod
	{
		ID,
		ID_SubPath,
		MatrixID_SubPath,
		OnlineScene_SubPath,
		PrefabTrackID,
		PrefabAllSpawnbleList,
		NULL
	}

	public struct PathData
	{
		public string Path;
		public string OnlineStartPath;
		public uint GameObject;
		public PathMethod PathMethod;
	}

	public static GameObject TraversePath(GameObject GameObject, string Path)
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


	public static GameObject GetObjectMessage(PathData msg)
	{
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
				NetworkedObject =
					TraversePath(
						msg.GameObject.NetIdToGameObject().GetComponent<MatrixSync>().NetworkedMatrix.gameObject,
						msg.Path);
				break;
			case PathMethod.OnlineScene_SubPath:
				NetworkedObject =
					TraversePath(
						UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
							.FirstOrDefault(x => x.name == msg.OnlineStartPath), msg.Path);
				break;
			case PathMethod.PrefabTrackID:
				NetworkedObject = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[msg.OnlineStartPath];
				break;
			case PathMethod.PrefabAllSpawnbleList:
				NetworkedObject = CustomNetworkManager.Instance.allSpawnablePrefabs[(int)(msg.GameObject)];
				break;
			case PathMethod.NULL:
				NetworkedObject = null;
				break;
		}

		return NetworkedObject;
	}


	public static PathData GetPathForMessage(GameObject InObject, string ValueName = null)
	{
		var Message = new PathData();

		if (InObject == null)
		{
			Message.PathMethod = PathMethod.NULL;
			return Message;
		}

		//Network object itself
		var NetworkIdentity = InObject.GetComponent<NetworkIdentity>();
		if (NetworkIdentity != null)
		{
			Message.GameObject = NetworkIdentity.netId;
		}

		Message.PathMethod = PathMethod.ID;
		if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return Message;

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

		if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return Message;
		//Tile map layers and above networking

		ExploringObject = InObject.transform;

		Path.Clear();
		Message.Path = "";

		if (ExploringObject.parent != null)
		{
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
		}
		else
		{
			var Net = ExploringObject.GetComponent<NetworkedMatrix>();
			if (Net != null)
			{
				Message.Path = JsonConvert.SerializeObject(Path);
				Message.PathMethod = PathMethod.MatrixID_SubPath;
				Message.GameObject = Net.MatrixSync.netId;
			}
		}

		Message.Path = JsonConvert.SerializeObject(Path);

		if (Message.GameObject is not (NetId.Empty or NetId.Invalid)) return Message;
		//Online scene Networking
		ExploringObject = InObject.transform;
		Message.Path = "";
		Path.Clear();
		while (ExploringObject.parent != null)
		{
			Path.Add(ExploringObject.GetSiblingIndex());
			ExploringObject = ExploringObject.transform.parent;
		}

		if (string.IsNullOrEmpty(ValueName) == false && InObject.transform == ExploringObject)
		{
			Message.OnlineStartPath = ValueName;
		}
		else
		{
			Message.OnlineStartPath = ExploringObject.name;
		}

		Message.Path = JsonConvert.SerializeObject(Path);
		Message.PathMethod = PathMethod.OnlineScene_SubPath;
		var Any = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
			.FirstOrDefault(x => x.name == Message.OnlineStartPath);

		if (Any != null)
		{
			return Message;
		}

		//is Prefab Most probably

		var TrackingID = InObject.GetComponent<PrefabTracker>();
		if (TrackingID != null)
		{
			Message.PathMethod = PathMethod.PrefabTrackID;
			Message.OnlineStartPath = TrackingID.ForeverID;
			return Message;
		}


		var Index = CustomNetworkManager.Instance.allSpawnablePrefabs.FindIndex(x => x == InObject);
		if (Index != -1)
		{
			Message.PathMethod = PathMethod.PrefabAllSpawnbleList;
			Message.GameObject = (uint) Index;
			return Message;
		}

		Message.PathMethod = PathMethod.NULL;
		return Message;
	}
}