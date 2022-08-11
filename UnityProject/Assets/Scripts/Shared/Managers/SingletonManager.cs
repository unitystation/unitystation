using UnityEngine;

namespace Shared.Managers
{
	/// <summary>
	/// Singleton Manager using static instances without use of FindObject
	/// If you are using Awake() override and remember to call base.Awake()!
	/// </summary>
	public class SingletonManager<T> : MonoBehaviour where T : MonoBehaviour
	{
		public static T Instance;


		/// <summary>
		/// If you override this then make sure you call base.Awake() somewhere in your Awake code.
		/// </summary>
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


		public virtual void Start()
		{
			Instance = this as T;
		}

		/// <summary>
		/// If you override this then make sure you call base.OnDestroy() somewhere in your OnDestroy code.
		/// </summary>
		public virtual void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
	}

}
