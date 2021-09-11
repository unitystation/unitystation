using UnityEngine;

namespace Managers
{
	/// <summary>
	/// Singleton manager using static instances without use of FindObject
	/// If you are using Awake() override remember to call base.Awake()!
	/// </summary>
	public class SingletonManager<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T instance;
		public static T Instance => instance;

		public virtual void Awake()
		{
			if (instance == null)
			{
				instance = this as T;
			}
			else
			{
				Destroy(gameObject);
			}
		}
	}
}
