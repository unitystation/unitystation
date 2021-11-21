using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Abstract class for making reload-proof singletons out of ScriptableObjects
	/// Returns the asset created on the editor, or null if there is none
	/// Based on https://www.youtube.com/watch?v=VBA1QCoEAX4
	/// </summary>
	/// <typeparam name="T">Singleton type</typeparam>

	public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
	{
		static T _instance = null;
		public static T Instance
		{
			get
			{
				if (_instance == null && (SOs.Instance == null) == false)
				{
					_instance = SOs.Instance.GetEntry<T>();
				}
				// SO might not be added to SOs manager or might be requested before the manager has awoken.
				var watch = Stopwatch.StartNew();
				if (_instance == null)
				{
					_instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
				}
				if (_instance == null)
				{
					_instance = Resources.LoadAll<T>("ScriptableObjectsSingletons").FirstOrDefault();
				}
				if (_instance == null)
				{
					Logger.LogErrorFormat("SingletonScriptableObject instance for {0} not found!", Category.Unknown, typeof(T));
				}
				watch.Stop();
				if (watch.ElapsedMilliseconds > 2)
				{
					Logger.LogWarning($"{typeof(T).FullName} SO took {watch.ElapsedMilliseconds} ms to find! " +
							$"Try to serialize a reference to this SO singleton instead!");
				}

				return _instance;
			}
		}
	}
}
