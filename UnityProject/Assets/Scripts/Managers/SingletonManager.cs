using Mirror;
using UnityEngine;

namespace Managers
{
	/// <summary>
	/// Singleton Manager using static instances without use of FindObject
	/// If you are using Awake() override and remember to call base.Awake()!
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

	/// <summary>
	/// Networked Singleton Manager using static instances without use of FindObject
	/// If you are using Awake() override and remember to call base.Awake()!
	/// </summary>
	public class NetworkedSingletonManager<T> : NetworkBehaviour where T : NetworkBehaviour
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
