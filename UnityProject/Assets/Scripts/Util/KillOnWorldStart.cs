using UnityEngine;

namespace Util
{
	public class KillOnWorldStart : MonoBehaviour {

		void Start () {
			Destroy(gameObject);
		}
	}
}
