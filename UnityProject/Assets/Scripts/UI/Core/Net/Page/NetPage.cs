using UnityEngine;

namespace UI.Core.NetUI
{
	public class NetPage : MonoBehaviour
	{
		public NetUIElementBase[] Elements => GetComponentsInChildren<NetUIElementBase>(false);
	}
}
