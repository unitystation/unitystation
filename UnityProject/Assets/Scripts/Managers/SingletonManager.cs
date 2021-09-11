using UnityEngine;

namespace Managers
{
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
