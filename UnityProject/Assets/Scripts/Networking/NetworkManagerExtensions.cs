using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Mirror;

public static class NetworkManagerExtensions
{
	/// <summary>
	///     Finds all classes derived from ClientMessage and registers their server handlers.
	/// </summary>
	public static void RegisterServerHandlers(this CustomNetworkManager manager)
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ClientMessage));
		MethodInfo mi = GetHandlerInfo();

		foreach (Type type in types)
		{
			MethodInfo method = mi.MakeGenericMethod(type);
			method.Invoke(null, new object[] {manager, null});
		}
	}

	/// <summary>
	///     Finds all classes derived from ServerMessage and registers their client handlers.
	/// </summary>
	public static void RegisterClientHandlers(this CustomNetworkManager manager, NetworkConnection conn)
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ServerMessage));
		MethodInfo mi = GetHandlerInfo();

		foreach (Type type in types)
		{
			MethodInfo method = mi.MakeGenericMethod(type);
			method.Invoke(null, new object[] {manager, conn});
		}
	}

	public static void RegisterHandler<T>(this CustomNetworkManager manager, NetworkConnection conn)
		where T : GameMessageBase, new()
	{
		// In normal C# this would just be `T.MessageType` but it seems unity's compiler has some stipulations about that...
		short msgType = new T().MessageType;
		NetworkMessageDelegate cb = delegate( NetworkMessage msg )
		{
			manager.StartCoroutine(msg.ReadMessage<T>().Process( msg.conn ));
		};

		if (conn != null)
		{
			conn.RegisterHandler(msgType, cb);
		}
		else
		{
			NetworkServer.RegisterHandler(msgType, cb);
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