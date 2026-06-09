using UnityEngine;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Core.Net.Page
{
	public class NetPage : MonoBehaviour
	{
		public NetUIElementBase[] Elements => GetComponentsInChildren<NetUIElementBase>(false);
	}
}
