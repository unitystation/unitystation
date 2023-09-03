using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Logs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Util
{
	public static class ComponentExtensions
	{
		/// <summary>
		/// Checks a component's reference to a non-child component to verify if it has been added properly. If a reference
		/// is not set, it will log the object/prefab and the component that has the missing reference. On a failed
		/// verification, a small component is attached to the object to track it and other failures. No further
		/// logging will occur on subsequent calls.
		/// </summary>
		/// <param name="parent">The container component with the reference to check.</param>
		/// <param name="component">A reference to a component to verify.</param>
		/// <param name="refDescription">A brief description of the missing reference.</param>
		/// <param name="refName">The name of the reference that is checked.</param>
		/// <param name="category">What to categorize the log as.</param>
		/// <typeparam name="V">The component's type.</typeparam>
		/// <returns>The component if the reference exists or it is found.</returns>
		public static V VerifyNonChildReference<V>(this Component parent, V component, string refDescription,
			[CallerMemberName] string refName = "", Category category = Category.Unknown) where V : Object
		{
			if (component != null) return component;

			var go = parent.gameObject;

			if (HasFailedReference(go, $"{parent.GetType()}-{refName}"))
			{
				return component;
			}

			var objName = go.name.RemoveClone();
			var description = MissingRefDescription(parent, objName, refName, refDescription, typeof(V));
			Loggy.LogError($"{description} Functionality may be hindered or broken.", category);

			return component;
		}

		/// <summary>
		/// Checks a component's reference to a child component to verify if it has been added properly. If a reference is
		/// not set, it will log the object/prefab and the component that has the missing reference. This will attempt to
		/// search the object's hierarchy for a given child name and add the reference. If a child name is not given, it
		/// will search using the name of the calling method (case-insensitive). Lastly, if no child matches the given
		/// name, it will attempt to add a reference to the first component with a matching type if no more than a
		/// single component is found.
		///
		/// If a reference fails verification, a small component is attached to the object to track it and
		/// other failures. No further logging or attempts to find another child will occur on subsequent calls.
		/// </summary>
		/// <param name="parent">The container component with the reference to check.</param>
		/// <param name="component">A reference to a component to verify.</param>
		/// <param name="refDescription">A brief description of the missing reference.</param>
		/// <param name="childName">The name of a child to search for if the reference is missing.</param>
		/// <param name="refName">The name of the reference that is checked.</param>
		/// <param name="category">What to categorize the log as.</param>
		/// <typeparam name="V">The component's type.</typeparam>
		/// <returns>The component if the reference exists or it is found.</returns>
		public static V VerifyChildReference<V>(this Component parent, ref V component, string refDescription,
			string childName = null, [CallerMemberName] string refName = "", Category category = Category.Unknown) where V : Object
		{
			if (component != null) return component;

			var go = parent.gameObject;

			if (HasFailedReference(go, $"{parent.GetType()}-{refName}"))
			{
				return component;
			}

			childName ??= refName;
			var objName = go.name.RemoveClone();
			var description = MissingRefDescription(parent, objName, refName, refDescription, typeof(V));
			var componentsFound = go.GetComponentsInChildren<V>(true);
			component = componentsFound.FirstOrDefault(c => c.name.ToLower() == childName?.ToLower());
			if (component == null && componentsFound.Length > 1)
			{
				Loggy.LogError($"{description} Found multiple children with the required component. Check the object/prefab and add a reference to one of them.", category);
			}
			else
			{
				component ??= componentsFound.FirstOrDefault();

				Loggy.LogError(
					component == null
						? $"{description} Unable to find a child object with a '{typeof(V)}' component. Functionality may be hindered or broken."
						: $"{description} Found '{component.name}' as a child in the object. Check the object/prefab and add a reference to '{component.name}'.",
					category);
			}

			return component;
		}

		#region VerifyReference Helpers

		private static string MissingRefDescription(Component parent, string objName, string refName, string refDescription, Type compType) =>
			$"Component '{parent.GetType()}' in object/prefab '{objName}' is missing a reference for '{refName}' to {refDescription} with '{compType}' component.";

		private static bool HasFailedReference(GameObject go, string referenceKey)
		{
			if (go.TryGetComponent<FailedReferences>(out var failures) == false)
			{
				failures = go.AddComponent<FailedReferences>();
			}
			else if (failures.Refs.Contains(referenceKey))
			{
				return true;
			}

			failures.Refs.Add(referenceKey);
			return false;
		}

		/// <summary>
		/// A simple helper component attached to a game object when reference verification fails.
		/// </summary>
		private class FailedReferences : MonoBehaviour
		{
			public readonly HashSet<string> Refs = new HashSet<string>();
		}

		#endregion
	}
}