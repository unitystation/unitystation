using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//TODO update mirror!!!!!!!!!!!, this is only here because mirror didn't update a synchronised list properly
public class PokeClientSubScene : ServerMessage
{
	public string ToLoadSceneName = "";
	public override void Process()
	{
		if (CustomNetworkManager.Instance._isServer) return;
		SubSceneManager.ManuallyLoadScene(ToLoadSceneName);
	}

	public static void SendToAll(string SceneName)
	{
		var msg = new PokeClientSubScene
		{
			ToLoadSceneName = SceneName
		};
		msg.SendToAll();
	}
}
