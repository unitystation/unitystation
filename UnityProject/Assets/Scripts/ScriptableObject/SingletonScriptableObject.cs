using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract class for making reload-proof singletons out of ScriptableObjects
/// Returns the asset created on the editor, or null if there is none
/// Based on https://www.youtube.com/watch?v=VBA1QCoEAX4
/// </summary>
/// <typeparam name="T">Singleton type</typeparam>

public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject {
	static T _instance = null;
	public static T Instance
	{
		get
		{
			if (!_instance)
			{
				_instance = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
			}
			if (!_instance)
			{
				_instance = Resources.LoadAll<T>("ScriptableObjects/SOs singletons").FirstOrDefault();
			}
			if (!_instance)
			{
				Logger.LogErrorFormat("SingletonScriptableObject instance for {0} not found!", Category.Unknown, typeof(T));
			}

			return _instance;
		}
	}
}