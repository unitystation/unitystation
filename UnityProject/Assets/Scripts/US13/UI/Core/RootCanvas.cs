using UnityEngine;

namespace US13.UI.Core
{
	public class RootCanvas : MonoBehaviour
	{
		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
