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
		public static T Instance { get; private set; }

		public virtual void Awake()
		{
			if (Instance == null)
			{
				Instance = this as T;
			}
			else
			{
				Destroy(gameObject);
			}
		}
	}
}
