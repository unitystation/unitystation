using UnityEngine;

namespace US13.UI.Core
{
	public class BringToFront : MonoBehaviour {
		void OnEnable()
		{
			transform.SetAsLastSibling();
		}
	}
}
