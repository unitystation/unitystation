using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using Messages;
using Messages.Client;
using Messages.Server;

public static class NetworkManagerExtensions
{
	/// <summary>
	/// Finds all classes derived from ClientMessage and registers their server handlers.
	/// </summary>
	public static void RegisterServerHandlers()
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ClientMessage<>));
		MethodInfo mi = GetHandlerInfo();

		foreach (var type in types)
		{
			MethodInfo method = mi.MakeGenericMethod(type, type.BaseType?.GenericTypeArguments[0]);
			method.Invoke(null, new object[] {true});
		}
	}

	/// <summary>
	/// Finds all classes derived from ServerMessage and registers their client handlers.
	/// </summary>
	public static void RegisterClientHandlers()
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ServerMessage<>));
		MethodInfo mi = GetHandlerInfo();

		foreach (var type in types)
		{
			MethodInfo method = mi.MakeGenericMethod(type, type.BaseType?.GenericTypeArguments[0]);
			method.Invoke(null, new object[] {false});
		}
	}

	public static void RegisterHandler<T, U>(bool isServer) where T : GameMessageBase<U>
		, new() where U : struct, NetworkMessage
	{
		var message = Activator.CreateInstance<T>();

		if (!isServer)
		{
			NetworkClient.RegisterHandler(new Action<NetworkConnection, U>(message.PreProcess));
		}
		else
		{
			NetworkServer.RegisterHandler(new Action<NetworkConnection, U>(message.PreProcess));
		}
	}

	/// <summary>
	///     Gets the method info for the RegisterHandler method above.
	/// </summary>
	private static MethodInfo GetHandlerInfo()
	{
		return typeof(NetworkManagerExtensions).GetMethod(nameof(RegisterHandler), BindingFlags.Static | BindingFlags.Public);
	}

	/// <summary>
	///     Finds all types that derive from the given type.
	/// </summary>
	private static IEnumerable<Type> GetDerivedTypes(Type baseType)
	{
		return Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOfOpen(baseType))
			.ToArray();
	}

	// https://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
	private static bool IsSubclassOfOpen(this Type t, Type baseType)
	{
		while (t != null && t != typeof(object))
		{
			Type cur = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
			if (baseType == cur)
			{
				return true;
			}
			t = t.BaseType;
		}

		return false;
	}
}
