using UnityEngine;

public class NetPage : MonoBehaviour
{
	public NetUIElement[] Elements => GetComponentsInChildren<NetUIElement>(false);
}
