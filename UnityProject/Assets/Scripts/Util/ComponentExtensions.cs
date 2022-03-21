using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Util
{
	public static class ComponentExtensions
	{
		/// <summary>
		/// Checks a component's reference to a non-child component to verify if it has been added properly. If a reference
		/// is not set, it will log the object/prefab and the component that has the missing reference.
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
			var objName = go.name.RemoveClone();
			var description = MissingRefDescription(parent, objName, refName, refDescription, typeof(V));
			Logger.LogError($"{description} Functionality may be hindered or broken.", category);

			return component;
		}

		/// <summary>
		/// Checks a component's reference to a child component to verify if it has been added properly. If a reference is
		/// not set, it will log the object/prefab and the component that has the missing reference. This will attempt to
		/// search the object's hierarchy for a given child name and add the reference. If a child name is not given, it
		/// will search using the name of the calling method (case-insensitive). Lastly, if no child matches the given
		/// name, it will attempt to add a reference to the first component with a matching type if no more than a
		/// single component is found.
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

			childName ??= refName;
			var go = parent.gameObject;
			var objName = go.name.RemoveClone();
			var description = MissingRefDescription(parent, objName, refName, refDescription, typeof(V));
			var componentsFound = go.GetComponentsInChildren<V>(true);
			component = componentsFound.FirstOrDefault(c => c.name.ToLower() == childName?.ToLower());
			if (component == null && componentsFound.Length > 1)
			{
				Logger.LogError($"{description} Found multiple children with the required component. Check the object/prefab and add a reference to one of them.", category);
			}
			else
			{
				component ??= componentsFound.FirstOrDefault();

				Logger.LogError(
					component == null
						? $"{description} Unable to find a child object with a '{typeof(V)}' component. Functionality may be hindered or broken."
						: $"{description} Found '{component.name}' as a child in the object. Check the object/prefab and add a reference to '{component.name}'.",
					category);
			}

			return component;
		}

		private static string MissingRefDescription(Component parent, string objName, string refName, string refDescription, Type compType) =>
			$"Component '{parent.GetType()}' in object/prefab '{objName}' is missing a reference for '{refName}' to {refDescription} with '{compType}' component.";
	}
}