using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace SecureStuff
{
	public class BaseAttribute : Attribute
	{
	}

	public struct MethodsAndAttributee<T> where T : BaseAttribute
	{
		public MethodInfo MethodInfo;
		public T Attribute;
	}

	public interface IAllowedReflection
	{
	}

	[System.Serializable]
	public class EventConnection
	{
		public Component TargetComponent;
		public string TargetFunction;

		public Component SourceComponent;
		public string SourceEvent;
	}


	public static class AllowedReflection
	{
		public static void RegisterNetworkMessages(Type messagebaseType, Type networkManagerExtensions,
			string registerMethodName, bool IsServer)
		{
			if (messagebaseType.GetInterfaces().Contains(typeof(IAllowedReflection)) == false)
			{
				Loggy.LogError(
					"RegisterNetworkMessages Got a message type that didn't Implement IAllowedReflection Interface");
				return;
			}

			var methodInfo =
				networkManagerExtensions.GetMethod(registerMethodName, BindingFlags.Static | BindingFlags.Public);

			if (methodInfo.GetCustomAttribute<BaseAttribute>(true) == null)
			{
				var VVNote = methodInfo.GetCustomAttribute<VVNote>(true);
				if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
				{
					Loggy.LogError(
						"registerMethod Wasn't marked with VVNote VVHighlight.SafeToModify100 or BaseAttribute Presumed unsafe");
					return;
				}
			}

			IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x =>
				x.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOfOpen(messagebaseType))).ToArray();

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

		public static object InvokeFunction(MethodInfo methodInfo, object instance, object[] parameters)
		{
			if (ValidateMethodInfo(methodInfo))
			{
				return methodInfo.Invoke(instance, parameters);
			}
			else
			{
				return null;
			}
		}

		public static T CreateInstance<T>() where T : IAllowedReflection, new()
		{
			return new T();
		}

		public static MethodInfo GetMethodInfo(Component NetworkObject, string MethodName)
		{
			var methodInfo = NetworkObject.GetType().GetMethod(MethodName);

			if (ValidateMethodInfo(methodInfo))
			{
				return methodInfo;
			}
			else
			{
				return null;
			}
		}


		public static EventInfo GetEventInfo(Component NetworkObject, string EventName)
		{
			var EventInfo = NetworkObject.GetType().GetEvent(EventName);

			if (EventInfo.GetCustomAttribute<BaseAttribute>(true) == null)
			{
				var VVNote = EventInfo.GetCustomAttribute<VVNote>(true);
				if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
				{
					Type declaringType = EventInfo.DeclaringType;

					if (declaringType == null || declaringType.IsSubclassOf(typeof(Component)) == false)
					{
						return null;
					}
				}
			}

			return EventInfo;
		}


		public static void PopulateEventRouter(EventConnection Connection)
		{
			if (string.IsNullOrEmpty(Connection.TargetFunction)) return;
			if (string.IsNullOrEmpty(Connection.SourceEvent)) return;
			if (Connection.SourceComponent == null) return;
			if (Connection.TargetComponent == null) return;

			var method = AllowedReflection.GetMethodInfo(Connection.TargetComponent, Connection.TargetFunction);
			if (method == null) return;

			var ActionListenerField = Connection.SourceComponent.GetType().GetField(Connection.SourceEvent);

			if (ActionListenerField != null)
			{
				if (ActionListenerField.GetCustomAttribute<BaseAttribute>(true) == null)
				{
					var VVNote = ActionListenerField.GetCustomAttribute<VVNote>(true);
					if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
					{
						if (ActionListenerField.IsStatic) return;
						if (ActionListenerField.IsPrivate) return;

						Type declaringType = ActionListenerField.DeclaringType;

						if (declaringType == null || declaringType.IsSubclassOf(typeof(Component)) == false)
						{
							return;
						}
					}
				}


				var ActionListenerObject = ActionListenerField.GetValue(Connection.SourceComponent);
				if (ActionListenerObject is UnityEvent UnityEvent)
				{
					var UnityAction =
						(UnityAction) Delegate.CreateDelegate(typeof(UnityAction), Connection.TargetComponent, method);
					UnityEvent.AddListener(UnityAction);
					return;
				}
			}
			else
			{
				var eventInfo = AllowedReflection.GetEventInfo(Connection.SourceComponent, Connection.SourceEvent);

				// Creating a new action for the method you want to add
				var Action = (Action) Delegate.CreateDelegate(typeof(Action), Connection.TargetComponent, method);


				// Getting the existing actions
				var existingDelegate = (Action) eventInfo.GetAddMethod(true)
					.Invoke(Connection.SourceComponent, new object[] {null});

				// Combining the existing actions with the new action
				Action combinedAction = (Action) Delegate.Combine(existingDelegate, Action);

				// Setting the event to the combined action
				eventInfo.GetAddMethod(true).Invoke(Connection.SourceComponent, new object[] {combinedAction});
			}
		}

		private static bool ValidateMethodInfo(MethodInfo methodInfo)
		{
			if (methodInfo.GetCustomAttribute<BaseAttribute>(true) == null)
			{
				var VVNote = methodInfo.GetCustomAttribute<VVNote>(true);
				if (VVNote is not {variableHighlightl: VVHighlight.SafeToModify100})
				{
					if (methodInfo.IsStatic) return false;
					if (methodInfo.IsPrivate) return false;

					Type declaringType = methodInfo.DeclaringType;

					if (declaringType == null || declaringType.IsSubclassOf(typeof(Component)) == false)
					{
						return false;
					}
				}
			}

			return true;
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


		public static Dictionary<Type, List<MethodsAndAttributee<T>>> GetFunctionsWithAttribute<T>()
			where T : BaseAttribute
		{
			var result = new Dictionary<Type, List<MethodsAndAttributee<T>>>();

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
				fieldInfo.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
					.FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
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