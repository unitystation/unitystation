using UnityEngine;

namespace US13.UI.Core
{
	/// <summary>
	/// Destroys gameobject whenever this gets disabled
	/// </summary>
	public class DestroyOnDisable : MonoBehaviour
	{
		private void OnDisable()
		{
			Destroy(gameObject);
		}
	}
}