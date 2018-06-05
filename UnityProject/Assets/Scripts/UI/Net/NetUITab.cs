using UnityEngine;
public class NetUITab : MonoBehaviour {
	public GameObject Provider;
	public TabType Type;
	protected NetworkTabInfo Info => NetworkTabManager.Instance.Get( Provider, Type );
}
