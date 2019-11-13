
using Boo.Lang;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utils related to finding stuff
/// </summary>
public static class FindUtils
{
	/// <summary>
	/// Special version of FindObjects which supports interfaces
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static List<T> FindInterfaceImplementersInScene<T>()
	{
		List<T> interfaces = new List<T>();
		GameObject[] rootGameObects = SceneManager.GetActiveScene().GetRootGameObjects();

		foreach (var rootGameObject in rootGameObects)
		{
			T[] childrenInterfaces = rootGameObject.GetComponentsInChildren<T>();
			foreach (var childInterface in childrenInterfaces)
			{
				interfaces.Add(childInterface);
			}
		}

		return interfaces;
	}
}
