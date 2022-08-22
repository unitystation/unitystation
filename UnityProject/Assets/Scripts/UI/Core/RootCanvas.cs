using UnityEngine;

namespace UI.Core
{
	public class RootCanvas : MonoBehaviour
	{
		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
