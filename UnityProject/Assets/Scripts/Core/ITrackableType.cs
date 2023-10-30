using System.Collections.Generic;
using System.Diagnostics;
using Logs;
using UnityEngine;

namespace Core
{
	public static class TrackableType<T>
	{
		public static List<T> Instances { get; } = new List<T>();

		public static List<T> GetAllNearbyTypesToTarget(GameObject target, float maximumDistance)
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
			List<T> components = new List<T>();
			foreach (var stationObject in Instances)
			{
				var obj = stationObject as Component;
				if (Vector3.Distance(obj.gameObject.AssumedWorldPosServer(), target.AssumedWorldPosServer()) > maximumDistance)
				{
					continue;
				}
				else
				{
					components.Add(stationObject);
				}
			}
#if UNITY_EDITOR
			stopwatch.Stop();
			Loggy.Log($"[GameObject/FindAllComponentsNearestToTarget<T>()] - Operation took {stopwatch.Elapsed.Milliseconds}ms");
#endif
			return components;
		}
	}
}