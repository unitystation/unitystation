using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class VendorTrigger : NetworkTabTrigger
{
	public List<VendorItem> VendorContent = new List<VendorItem>();
	[HideInInspector]
	public GameObject Originator;
	public Color HullColor;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (CustomNetworkManager.Instance._isServer)
			Originator = originator;
		return base.Interact(originator, position, hand);
	}
}

public enum EjectDirection { None, Up, Down, Random }

//Adding this as a separate class so we can easily extend it in future -
//add price or required access, stock amount and etc.
[System.Serializable]
public class VendorItem
{
	public GameObject item;
}