using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests
{
	[Flags]
	public enum ReferenceStatus
	{
		None = 0,
		Null = 1 << 0,
		Missing = 1 << 1,
		Object = 1 << 2
	}

	/// <summary>
	/// Creating and iterating through <see cref="SerializedObject"/> is quite slow when we are checking tens of
	/// thousands of objects/components. If we are only checking fields with serialized Unity Objects to see if they
	/// are none or missing, then we can cache those fields instead. This class handles creating the
	/// <see cref="FieldInfo"/> lists for a type as needed and safely handles checking for null/missing
	/// references on instances.
	/// needed.
	/// </summary>
	public class SerializedObjectFieldsMap
	{
		private Dictionary<Type, List<FieldInfo>> Map { get; } = new();

		private IReadOnlyList<FieldInfo> GetFieldsFor(Object instance)
		{
			var type = Utils.GetObjectType(instance);

			if (Map.TryGetValue(type, out var fieldsInfo)) return fieldsInfo;

			fieldsInfo = CreateSerializedFieldsInfo(instance);
			Map.Add(type, fieldsInfo);

			return fieldsInfo;
		}

		private static List<FieldInfo> CreateSerializedFieldsInfo(Object @object)
		{
			var type = @object.GetType();
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			return fields.Where(f =>
			{
				var isUnityObject = typeof(Object).IsAssignableFrom(f.FieldType);
				var isSerialized = f.IsPublic || f.GetCustomAttribute(typeof(SerializeField)) != null;
				return isUnityObject && isSerialized;
			}).ToList();
		}


		/// <summary>
		/// Checks and returns the field's reference status. A field that isn't null will return Object status.
		/// If the reference is considered Unity's null, then attempt to get the instance ID from the value.
		/// If that ID is not 0, then it means the reference is missing. Otherwise the reference is Null/None.
		/// </summary>
		public static ReferenceStatus GetReferenceStatus(FieldInfo field, object instance, bool CareAboutNull)
		{
			var value = field.GetValue(instance) as Object;

			if (value != null) return ReferenceStatus.Object;


			// At this point, value is Unity's null but the object may still actually exist.
			var Status = Utils.GetInstanceID(value) != 0 ? ReferenceStatus.Missing : ReferenceStatus.Null;

			if (Status == ReferenceStatus.Null && CareAboutNull == false)
			{
				return ReferenceStatus.Object;
			}

			return Status;
		}


		public IEnumerable<(string name, ReferenceStatus status)> FieldNamesWithStatus(object instance,
			ReferenceStatus status, HashSet<int> visited = null)
		{
			if (instance == null) yield break;

			bool CareAboutNulls = true;
			if (visited != null)
			{
				CareAboutNulls = false;
			}

			// Initialize visited set if not provided
			visited ??= new HashSet<int>();

			// Avoid self-referential loops by checking if the object is already visited
			if (!visited.Add(instance.GetHashCode())) yield break;

			foreach (var field in GetSerializableFieldsFor(instance))
			{
				var fieldValue = field.GetValue(instance);
				var fieldStatus = GetReferenceStatus(field, instance, CareAboutNulls);

				if (fieldValue != null && typeof(IEnumerable).IsAssignableFrom(field.FieldType) &&
				    field.FieldType != typeof(string))
				{
// Handle collections separately
					if (fieldValue is IList collection)
					{
						if (collection == null) continue;
						int index = 0;
						foreach (var item in collection)
						{
							if (IsObjectReference(field.FieldType) == false)
							{
								foreach (var nestedResult in FieldNamesWithStatus(item, status, visited))
								{
									yield return ($"{field.Name}[{index}].{nestedResult.name}", nestedResult.status);
								}
							}

							index++;
						}
					}
				}
				else
				{
					if ((fieldStatus & status) != 0)
					{
						yield return (field.Name, fieldStatus);
					}

					if (fieldValue != null)
					{
						if (IsObjectReference(field.FieldType) == false)
						{
							// Recursively handle nested objects
							foreach (var nestedResult in FieldNamesWithStatus(fieldValue, status, visited))
							{
								yield return ($"{field.Name}.{nestedResult.name}", nestedResult.status);
							}
						}
					}
				}
			}

			// Remove the object from visited set after processing to allow other instances of the same object to be processed
			visited.Remove(instance.GetHashCode());
		}

		private static IEnumerable<FieldInfo> GetSerializableFieldsFor(object instance)
		{
			return instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(field => field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
				.Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null && field.GetCustomAttribute<HideInInspector>() == null)
				.Where(field => field.FieldType.IsValueType == false && field.FieldType.IsPrimitive == false &&
				                field.FieldType != typeof(string))
				.Where(field => IsGenericTypeOrContainsInvalidGenericArguments(field.FieldType));
		}

		private static bool IsObjectReference(Type fieldType)
		{
			if (fieldType.IsGenericType)
			{
				// Check if any of the generic arguments are invalid
				foreach (var arg in fieldType.GetGenericArguments())
				{
					if (typeof(UnityEngine.Object).IsAssignableFrom(arg))
					{
						return true;
					}
				}

				return false;
			}
			else
			{
				return typeof(UnityEngine.Object).IsAssignableFrom(fieldType);
			}
		}

		private static bool IsGenericTypeOrContainsInvalidGenericArguments(Type fieldType)
		{
			// Traverse the inheritance hierarchy to check if any base type is generic
			Type currentType = fieldType;
			while (currentType != null)
			{
				// Check if any of the generic arguments are invalid
				if (currentType.IsGenericType)
				{
					foreach (var arg in currentType.GetGenericArguments())
					{
						if (typeof(UnityEngine.Object).IsAssignableFrom(arg))
						{
							if (!(typeof(IEnumerable).IsAssignableFrom(arg) && arg != typeof(string)))
							{
								return false;
							}
						}

						if (arg.IsValueType || arg.IsPrimitive || arg == typeof(string))
						{
							return false;
						}
					}
				}

				currentType = currentType.BaseType;
			}


			return true;
		}
	}
}