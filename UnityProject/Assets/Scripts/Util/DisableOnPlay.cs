using UnityEngine;

namespace Util
{
	public class DisableOnPlay : MonoBehaviour
	{
		private void Start()
		{
			gameObject.SetActive(false);
		}
	}
}