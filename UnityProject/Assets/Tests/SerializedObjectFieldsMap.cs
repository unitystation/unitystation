using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		/// Gather all field names and their status from an instance that match the given reference status.
		/// </summary>
		public IEnumerable<(string name, ReferenceStatus status)> FieldNamesWithStatus(Object instance, ReferenceStatus status)
		{
			if (instance == null) yield break;

			foreach (var field in GetFieldsFor(instance))
			{
				var fieldStatus = GetReferenceStatus(field, instance);

				if ((fieldStatus & status) == 0) continue;

				yield return (field.Name, fieldStatus);
			}
		}

		/// <summary>
		/// Checks and returns the field's reference status. A field that isn't null will return Object status.
		/// If the reference is considered Unity's null, then attempt to get the instance ID from the value.
		/// If that ID is not 0, then it means the reference is missing. Otherwise the reference is Null/None.
		/// </summary>
		private static ReferenceStatus GetReferenceStatus(FieldInfo field, Object instance)
		{
			var value = field.GetValue(instance) as Object;

			if (value != null) return ReferenceStatus.Object;

			// At this point, value is Unity's null but the object may still actually exist.
			return Utils.GetInstanceID(value) != 0 ? ReferenceStatus.Missing : ReferenceStatus.Null;
		}
	}
}