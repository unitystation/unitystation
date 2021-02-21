using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
//TODO update mirror!!!!!!!!!!!, this is only here because mirror didn't update a synchronised list properly
public class PokeClientSubScene : ServerMessage
{
	public class PokeClientSubSceneNetMessage : NetworkMessage
	{
		public string ToLoadSceneName = "";
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PokeClientSubSceneNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (CustomNetworkManager.Instance._isServer) return;
		SubSceneManager.ManuallyLoadScene(newMsg.ToLoadSceneName);
	}

	public static void SendToAll(string SceneName)
	{
		var msg = new PokeClientSubSceneNetMessage
		{
			ToLoadSceneName = SceneName
		};
		new PokeClientSubScene().SendToAll(msg);
	}
}
