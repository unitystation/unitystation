using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Messages.Client;
using Mirror;
using UnityEngine;

public static class NetworkManagerExtensions
{
	/// <summary>
	///     Finds all classes derived from ClientMessage and registers their server handlers.
	/// </summary>
	public static void RegisterServerHandlers()
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ClientMessage));
		MethodInfo mi = GetHandlerInfo();

		foreach (Type type in types)
		{
			foreach (var fieldInfo in type.GetFields())
			{
				var fieldType = fieldInfo.FieldType;

				//Debug.LogError($"{type} {fieldType}");

				if(IsSubclassOfOpen(fieldType, typeof(NetworkMessage)) == false) continue;

				Debug.LogError($"{fieldType} passed");

				//var messageBase = new Type(type);
				MethodInfo method = mi.MakeGenericMethod(fieldType);
				method.Invoke(null, new object[] {true, type, fieldType});
			}
		}
	}

	/// <summary>
	/// Finds all classes derived from ServerMessage and registers their client handlers.
	/// </summary>
	public static void RegisterClientHandlers()
	{
		IEnumerable<Type> types = GetDerivedTypes(typeof(ServerMessage));
		MethodInfo mi = GetHandlerInfo();

		foreach (Type type in types)
		{
			foreach (var fieldInfo in type.GetFields())
			{
				var fieldType = fieldInfo.FieldType;

				Debug.LogError($"{type} {fieldType}");

				if(IsSubclassOfOpen(fieldType, typeof(NetworkMessage)) == false) continue;

				Debug.LogError($"{fieldType} passed");

				MethodInfo method = mi.MakeGenericMethod(fieldType);
				method.Invoke(null, new object[] {true, type, fieldType});
			}
		}
	}

	public static void RegisterHandler<T, U>(bool isServer, U messageBase)
		where T : NetworkMessage, new() where U : GameMessageBase
	{
		var message = (U)Activator.CreateInstance(messageBase.GetType());

		Debug.LogError($"{new T()} {message}");

		if (!isServer)
		{
			NetworkClient.RegisterHandler<T>(new Action<NetworkConnection, T>(message.PreProcess));
		}
		else
		{
			NetworkServer.RegisterHandler<T>(new Action<NetworkConnection, T>(message.PreProcess));
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