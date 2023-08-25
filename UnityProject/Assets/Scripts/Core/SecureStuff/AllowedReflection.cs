using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SecureStuff
{
	public class BaseAttribute : Attribute { }

	public struct MethodsAndAttributee<T> where T : BaseAttribute
	{
		public MethodInfo MethodInfo;
		public T Attribute;
	}

	public static class AllowedReflection
	{
		public static object InvokeFunction(MethodInfo methodInfo, object instance , object[] parameters)
		{
			if (methodInfo.GetCustomAttribute<BaseAttribute>(true) == null)
			{
				var VVNote = methodInfo.GetCustomAttribute<VVNote>(true);
				if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
				{
					if (methodInfo.IsStatic) return null;
					if (methodInfo.IsPrivate) return null;

					Type declaringType = methodInfo.DeclaringType;

					if (declaringType == null || declaringType.IsSubclassOf(typeof(MonoBehaviour)) == false)
					{
						return null;
					}
				}

			}

			return methodInfo.Invoke(instance, parameters);;
		}

		public static void ChangeVariableClient(GameObject NetworkObject, string MonoBehaviourName, string ValueName,
			string Newvalue, bool IsInvokeFunction)
		{
			//No statics!
			//Private fields/properties set good?
			//public functions good, Private  requires VV attribute, No return

			//hummmmmm SO Nonstatic %100
			//Private yeah
			//so What they want access to
			//Token/Account
			//File access

			var workObject =
				NetworkObject.GetComponent(MonoBehaviourName.Substring(MonoBehaviourName.LastIndexOf('.') + 1));
			var Worktype = workObject.GetType();

			if (IsInvokeFunction == false)
			{
				var infoField = Worktype.GetField(ValueName);

				if (infoField != null)
				{
					if (infoField.IsStatic) return;
					infoField.SetValue(workObject,
						Librarian.Page.DeSerialiseValue(workObject, Newvalue, infoField.FieldType));
					return;
				}


				var infoProperty = Worktype.GetProperty(ValueName);
				if (infoProperty != null)
				{
					var Method = infoProperty.GetSetMethod();
					if (Method == null) return;
					if (Method.IsStatic) return;
					infoProperty.SetValue(workObject,
						Librarian.Page.DeSerialiseValue(workObject, Newvalue, infoProperty.PropertyType));
					return;
				}
			}
			else
			{
				var Method = Worktype.GetMethod(ValueName);
				_ = InvokeFunction(Method, workObject, null);
			}
		}


		public static Dictionary<Type,List<MethodsAndAttributee<T>>> GetFunctionsWithAttribute<T>() where T : BaseAttribute
		{
			if (typeof(T) == typeof(ObsoleteAttribute))
			{
				Logger.LogError("hey no, no obsolete stuff");
				return null;
			}

			var result = new Dictionary<Type,List<MethodsAndAttributee<T>>>();

			var allComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(s => typeof(MonoBehaviour).IsAssignableFrom(s));

			foreach (var componentType in allComponentTypes)
			{
				var attributedMethodsForType = componentType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic |
				                                                        BindingFlags.Public |
				                                                        BindingFlags.FlattenHierarchy)
					.Where(m => m.GetCustomAttribute<T>(true) != null)
					.ToList();
				foreach (var Method in attributedMethodsForType)
				{
					if (result.ContainsKey(componentType) == false)
					{
						result[componentType] = new List<MethodsAndAttributee<T>>();
					}
					result[componentType].Add(new MethodsAndAttributee<T>()
					{
						MethodInfo = Method,
						Attribute = Method.GetCustomAttribute<T>(true)
					});
				}
			}

			return result;
		}
	}
}


