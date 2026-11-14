using System;
using System.Linq;
using UnityEngine;

namespace US13.HealthV2.Living
{
	public static class BodyPartBehaviorExtensions
	{
		public static T GetBodyPartBehavior<T>(this GameObject gameObject) where T : class
		{
			if (gameObject == null) return null;
			var host = gameObject.GetComponent<BodyPartBehaviors>();
			if (host == null) return null;
			if (gameObject.scene.IsValid()) host.Initialize();
			return host.Behaviors.OfType<T>().FirstOrDefault();
		}

		public static T GetBodyPartBehavior<T>(this IBodyPartBehavior behavior) where T : class
		{
			return behavior?.RelatedPart.GetBodyPartBehavior<T>();
		}

		public static bool TryGetBodyPartBehavior<T>(this IBodyPartBehavior source, out T behavior) where T : class
		{
			behavior = source.GetBodyPartBehavior<T>();
			return behavior != null;
		}

		public static T GetBodyPartBehavior<T>(this Component component) where T : class
		{
			return component == null ? null : component.gameObject.GetBodyPartBehavior<T>();
		}

		public static bool TryGetBodyPartBehavior<T>(this GameObject gameObject, out T behavior) where T : class
		{
			behavior = gameObject.GetBodyPartBehavior<T>();
			return behavior != null;
		}

		public static bool TryGetBodyPartBehavior<T>(this Component component, out T behavior) where T : class
		{
			return (component == null ? null : component.gameObject).TryGetBodyPartBehavior(out behavior);
		}

		public static T[] GetBodyPartBehaviors<T>(this Component component) where T : class
		{
			return component.gameObject.GetBodyPartBehaviors<T>();
		}

		public static T[] GetBodyPartBehaviors<T>(this GameObject gameObject) where T : class
		{
			return gameObject.GetComponent<BodyPartBehaviors>()?.Behaviors.OfType<T>().ToArray() ?? Array.Empty<T>();
		}

		public static T[] GetBodyPartBehaviorsInChildren<T>(this GameObject gameObject, bool includeInactive = false)
			where T : class
		{
			return gameObject.GetComponentsInChildren<BodyPartBehaviors>(includeInactive)
				.SelectMany(host => host.Behaviors).OfType<T>().ToArray();
		}

		public static T[] GetBodyPartBehaviorsInChildren<T>(this Component component, bool includeInactive = false)
			where T : class
		{
			return component.gameObject.GetBodyPartBehaviorsInChildren<T>(includeInactive);
		}

		/// <summary>
		/// Includes managed behaviors when discovering inventory, action, and possession interfaces.
		/// </summary>
		public static T[] GetComponentsAndBodyPartBehaviors<T>(this GameObject gameObject) where T : class
		{
			return typeof(IBodyPartBehavior).IsAssignableFrom(typeof(T))
				? gameObject.GetBodyPartBehaviors<T>()
				: gameObject.GetComponents<T>().Concat(gameObject.GetBodyPartBehaviors<T>()).ToArray();
		}

		public static T[] GetComponentsAndBodyPartBehaviors<T>(this Component component) where T : class
		{
			return component.gameObject.GetComponentsAndBodyPartBehaviors<T>();
		}

		public static T GetComponentOrBodyPartBehavior<T>(this GameObject gameObject) where T : class
		{
			return typeof(IBodyPartBehavior).IsAssignableFrom(typeof(T))
				? gameObject.GetBodyPartBehavior<T>()
				: gameObject.GetComponent<T>() ?? gameObject.GetBodyPartBehavior<T>();
		}

		public static T GetComponentOrBodyPartBehavior<T>(this Component component) where T : class
		{
			return component.gameObject.GetComponentOrBodyPartBehavior<T>();
		}
	}
}