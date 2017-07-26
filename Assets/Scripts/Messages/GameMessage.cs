using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameMessageBase : MessageBase {
	protected static short msgTypeCounter = 1000;

	public GameObject NetworkObject;
	public GameObject[] NetworkObjects;

	protected IEnumerator WaitFor(NetworkInstanceId id) {
		while ((NetworkObject = ClientScene.FindLocalObject(id)) == null) {
			yield return YieldHelper.EndOfFrame;
		}
	}

	protected IEnumerator WaitFor(params NetworkInstanceId[] ids) {
		NetworkObjects = new GameObject[ids.Length];

		while (!AllLoaded(ids)) {
			yield return YieldHelper.EndOfFrame;
		}
	}

	bool AllLoaded(NetworkInstanceId[] ids) {
		for (int i = 0; i < ids.Length; i++) {
			var obj = ClientScene.FindLocalObject(ids[i]);
			if (obj == null)
				return false;

			NetworkObjects[i] = obj;
		}

		return true;
	}
}

public abstract class GameMessage<T> : GameMessageBase {
	public static readonly short MessageType;

	static GameMessage()
	{
		// Each message needs to have a unique MessageType, defined as a short int.
		// Rather than have people manually define them and risk somebody using the
		// same number for two different messages, this constructor will automatically
		// assign the message types for you. Normally, static fields in C# classes are
		// not copied to derived classes, however, if you add a generic type parameter,
		// each type will get its own copy of the static field. So we add a type parameter
		// to GameMessage<T> and simply pass in the derived class as the parameter.
		// It's a hack, but it effectively turns a forgetful programmer mistake from being 
		// a runtime error into being a compile-time error.
		MessageType = GameMessageBase.msgTypeCounter++;
	}
		
	public abstract IEnumerator Process();

	public void Send() {
		try {
			Debug.Log("Send");
			Debug.Log(CustomNetworkManager.Instance.client.connection);
			CustomNetworkManager.Instance.client.Send(MessageType, this);
		} catch (Exception ex) {
			throw new InvalidOperationException("Could not send game message: " + this, ex);
		}
	}

	public void SendUnreliable() {
		try {
			CustomNetworkManager.Instance.client.SendUnreliable(MessageType, this);
		} catch (Exception ex) {
			throw new InvalidOperationException("Could not send game message: " + this, ex);
		}
	}
}
