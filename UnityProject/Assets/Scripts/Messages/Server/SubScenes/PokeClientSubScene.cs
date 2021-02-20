using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//TODO update mirror!!!!!!!!!!!, this is only here because mirror didn't update a synchronised list properly
public class PokeClientSubScene : ServerMessage
{
	public class PokeClientSubSceneNetMessage : ActualMessage
	{
		public string ToLoadSceneName = "";
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as PokeClientSubSceneNetMessage;
		if(newMsg == null) return;

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
