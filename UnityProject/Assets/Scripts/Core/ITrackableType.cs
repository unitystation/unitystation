using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Logs;
using UnityEngine;

namespace Core
{
	public static class ComponentsTracker<T>
	{
		public static HashSet<T> Instances { get; } = new HashSet<T>();

		public static List<T> GetAllNearbyTypesToTarget(GameObject target, float maximumDistance, bool bypassInventories = true)
		{
			if (Instances == null || Instances.Count == 0)
			{
				Loggy.Log($"No elements found for Type {nameof(T)}, are you sure you have ITrackableType<T> added to your class?");
				return null;
			}
#if UNITY_EDITOR
			var stopwatch = new Stopwatch();
			stopwatch.Start();
#endif
			List<T> components = GetNearbyComponents(bypassInventories, target.AssumedWorldPosServer(), maximumDistance);
#if UNITY_EDITOR
			stopwatch.Stop();
			Loggy.Log($"[GameObject/FindAllComponentsNearestToTarget<T>()] - Operation took {stopwatch.Elapsed.Milliseconds}ms");
#endif
			return components;
		}

		public static List<T> GetAllNearbyTypesToLocation(Vector3 target, float maximumDistance, bool bypassInventories = true)
		{
			if (Instances == null || Instances.Count == 0)
			{
				Loggy.Log($"No elements found for Type {nameof(T)}, are you sure you have ITrackableType<T> added to your class?");
				return null;
			}
#if UNITY_EDITOR
			var stopwatch = new Stopwatch();
			stopwatch.Start();
#endif
			List<T> components = GetNearbyComponents(bypassInventories, target, maximumDistance);
#if UNITY_EDITOR
			stopwatch.Stop();
			Loggy.Log($"[GameObject/FindAllComponentsNearestToTarget<T>()] - Operation took {stopwatch.Elapsed.Milliseconds}ms");
#endif
			return components;
		}

		public static List<ItemTrait> GetNearbyTraits(GameObject target, float searchRadius, bool bypassInventories = true)
		{
			var items = ComponentsTracker<Attributes>.GetAllNearbyTypesToTarget(target, searchRadius, bypassInventories);
			var traits = new List<ItemTrait>();
			foreach (var item in items)
			{
				traits.AddRange(item.InitialTraits);
			}
			return traits.Distinct().ToList();
		}

		private static List<T> GetNearbyComponents(bool bypassInventories, Vector3 target, float maximumDistance)
		{
			var components = new List<T>();
			foreach (var stationObject in Instances)
			{
				var obj = stationObject as Component;
				if (obj == null || obj.gameObject == null || obj.gameObject.OrNull() == null) continue;
				if (bypassInventories == false && obj.gameObject.IsAtHiddenPos())
				{
					continue;
				}
				if (Vector3.Distance(obj.gameObject.AssumedWorldPosServer(), target) > maximumDistance)
				{
					continue;
				}
				else
				{
					components.Add(stationObject);
				}
			}
			return components;
		}
	}
}