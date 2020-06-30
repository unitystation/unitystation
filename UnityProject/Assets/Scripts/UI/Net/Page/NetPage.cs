using UnityEngine;

public class NetPage : MonoBehaviour
{
	public NetUIElementBase[] Elements => GetComponentsInChildren<NetUIElementBase>(false);
}
