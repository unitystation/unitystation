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

	/// <summary>
	/// Networked Singleton Manager using static instances without use of FindObject
	/// If you are using Awake() override and remember to call base.Awake()!
	///
	/// Managers shouldn't be networked really, try to avoid
	/// </summary>
	public class SingletonNetworkedManager<T> : NetworkBehaviour where T : NetworkBehaviour
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
