using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using NUnit.Framework;
using UnityEngine;

namespace SecureStuff
{
	public class BaseAttribute : Attribute { }

	public struct MethodsAndAttributee<T> where T : BaseAttribute
	{
		public MethodInfo MethodInfo;
		public T Attribute;
	}

	public interface IAllowedReflection{}

	public static class AllowedReflection
	{

		public static void RegisterNetworkMessages(Type messagebaseType, Type networkManagerExtensions, string registerMethodName, bool IsServer)
		{
			if (messagebaseType.GetInterfaces().Contains(typeof(IAllowedReflection)) == false)
            {
            	Loggy.LogError("RegisterNetworkMessages Got a message type that didn't Implement IAllowedReflection Interface");
            	return;

			}

			var methodInfo = networkManagerExtensions.GetMethod(registerMethodName, BindingFlags.Static | BindingFlags.Public);

			if (methodInfo.GetCustomAttribute<BaseAttribute>(true) == null)
			{
				var VVNote = methodInfo.GetCustomAttribute<VVNote>(true);
				if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
				{
					Loggy.LogError("registerMethod Wasn't marked with VVNote VVHighlight.SafeToModify100 or BaseAttribute Presumed unsafe");
					return;
				}
			}

			IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOfOpen(messagebaseType))).ToArray();

			foreach (var type in types)
			{
				var message = Activator.CreateInstance(type);
				MethodInfo method = methodInfo.MakeGenericMethod(type, type.BaseType?.GenericTypeArguments[0]);
				method.Invoke(null, new object[] {IsServer, message});
			}
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

		public static T CreateInstance<T>() where T : IAllowedReflection, new()
		{
			return new T();
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
				Loggy.LogError("hey no, no obsolete stuff");
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

		public static string GetDescription(this Enum value)
		{
			FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
			if (fieldInfo == null)
			{
				return null;
			}
			System.ComponentModel.DescriptionAttribute descriptionAttribute =
				fieldInfo.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
			return descriptionAttribute == null ? value.ToString() : descriptionAttribute.Description;
		}
		public static int GetOrder(this Enum value)
		{
			FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
			OrderAttribute attribute =
				fieldInfo.GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() as OrderAttribute;
			return attribute == null ? -1 : attribute.Order;
		}

	}
}


