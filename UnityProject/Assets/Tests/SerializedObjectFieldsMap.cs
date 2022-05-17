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
		Missing = 1 << 0,
		Object = 1 << 1
	}

	/// <summary>
	/// Creating and iterating through <see cref="SerializedObject"/> is quite slow when we are checking tens of
	/// thousands of objects. If we are only checking fields with serialized Unity Objects to see if they are none or
	/// missing, then we can cache those fields instead. This class handles creating the <see cref="FieldInfo"/>
	/// lists for a type as needed and safely handles checking for null/missing references on instances.
	/// needed.
	/// </summary>
	public class SerializedObjectFieldsMap
	{
		private Dictionary<Type, List<FieldInfo>> Map { get; } = new();

		private IReadOnlyList<FieldInfo> this[Object instance]
		{
			get
			{
				var type = Utils.GetObjectType(instance);

				if (Map.TryGetValue(type, out var fieldsInfo)) return fieldsInfo;

				fieldsInfo = CreateSerializedFieldsInfo(instance);
				Map.Add(type, fieldsInfo);

				return fieldsInfo;
			}
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
		/// Gather all field names on an instance that match the given reference status.
		/// </summary>
		public IEnumerable<string> FieldNamesWithStatus(Object instance, ReferenceStatus status)
		{
			if (instance == null) yield break;

			foreach (var field in this[instance])
			{
				if ((GetReferenceStatus(field, instance) & status) == 0) continue;

				yield return field.Name;
			}
		}

		private static ReferenceStatus GetReferenceStatus(FieldInfo field, Object instance)
		{
			var value = field.GetValue(instance) as Object;

			if (value != null) return ReferenceStatus.Object;

			return Utils.GetInstanceID(value) != 0 ? ReferenceStatus.Missing : ReferenceStatus.None;
		}
	}
}