using UnityEngine;
public class NetUITab : MonoBehaviour {
	public GameObject Provider;
	public TabType Type;
	protected NetworkTabInfo Info => NetworkTabManager.Instance.Get( NetworkTab );
	public NetworkTab NetworkTab => new NetworkTab(Provider, Type);
}
