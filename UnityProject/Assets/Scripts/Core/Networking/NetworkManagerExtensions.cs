using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Networking;
using Mirror;
using Messages;
using Messages.Client;
using Messages.Server;
using SecureStuff;

public static class NetworkManagerExtensions
{
	/// <summary>
	/// Finds all classes derived from ClientMessage and registers their server handlers.
	/// </summary>
	public static void RegisterServerHandlers()
	{
		AllowedReflection.RegisterNetworkMessages(typeof(ClientMessage<>), typeof(NetworkManagerExtensions), nameof(RegisterHandler), true);

		// IEnumerable<Type> types = GetDerivedTypes(typeof(ClientMessage<>)); //so get all Implementations of ClientMessage Needed 100%
		// MethodInfo mi = GetHandlerInfo(); //
		//
		// foreach (var type in types)
		// {
		// 	MethodInfo method = mi.MakeGenericMethod(type, type.BaseType?.GenericTypeArguments[0]);
		// 	method.Invoke(null, new object[] {true});
		// }
	}

	/// <summary>
	/// Finds all classes derived from ServerMessage and registers their client handlers.
	/// </summary>
	public static void RegisterClientHandlers()
	{
		AllowedReflection.RegisterNetworkMessages(typeof(ServerMessage<>), typeof(NetworkManagerExtensions), nameof(RegisterHandler), false);

		// IEnumerable<Type> types = GetDerivedTypes(typeof(ServerMessage<>));
		// MethodInfo mi = GetHandlerInfo();
		//
		// foreach (var type in types)
		// {
		// 	MethodInfo method = mi.MakeGenericMethod(type, type.BaseType?.GenericTypeArguments[0]);
		// 	method.Invoke(null, new object[] {false});
		// }
	}

	[Base]
	public static void RegisterHandler<T, U>(bool isServer,T message ) where T : GameMessageBase<U>
		, new() where U : struct, NetworkMessage
	{
		if (!isServer)
		{
			NetworkClient.RegisterHandler(new Action<NetworkConnection, U>(message.PreProcess));
		}
		else
		{
			NetworkServer.RegisterHandler(new Action<NetworkConnection, U>(message.PreProcess));
		}
	}
}
