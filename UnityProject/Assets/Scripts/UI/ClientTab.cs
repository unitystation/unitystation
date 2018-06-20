using UnityEngine;

public class ClientTab : MonoBehaviour {
	public ClientTabType Type;
}

public enum ClientTabType {
	stats,
	itemList,
	options,
	more
}