using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;

public static class NetworkManagerExtensions {
	public static void RegisterHandler<T>(this NetworkManager manager, NetworkConnection conn) where T : GameMessage<T>, new() {
		// In normal C# this would just be `T.MessageType` but it seems unity's compiler has some stipulations about that...
		FieldInfo field = typeof(T).BaseType.GetField("MessageType", BindingFlags.Static | BindingFlags.Public);
		NetworkMessageDelegate cb = delegate(NetworkMessage msg) {
			manager.StartCoroutine(((GameMessage<T>)msg.ReadMessage<T>()).Process());
		};

		conn.RegisterHandler((short)field.GetValue(null), cb);
	}
}
