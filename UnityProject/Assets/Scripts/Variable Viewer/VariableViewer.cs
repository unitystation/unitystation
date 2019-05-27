using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public static class VariableViewer
{
	public static HashSet<Type> DeprecatedTypes = new HashSet<Type>()
	{
		typeof(Rigidbody)
	};

	public static void PrintSomeVariables(GameObject _object)
	{
		MonoBehaviour[] scriptComponents = _object.GetComponents<MonoBehaviour>();

		//For each monoBehaviour in the list of script components
		foreach (MonoBehaviour mono in scriptComponents)
		{
			Type monoType = mono.GetType();
			//foreach (MethodInfo method in monoType.GetMethods()) // BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
			//{
			//	Logger.Log(method.Name + " < this ");
			//}
			foreach (PropertyInfo method in monoType.GetProperties()) // BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
			{
				if (!DeprecatedTypes.Contains(method.GetType()))
				{
					Logger.Log(method + " < this " + method.GetValue(mono));
				}
				else {
					Logger.LogWarning("court" + method);
				}
			}


		}
	}
}

