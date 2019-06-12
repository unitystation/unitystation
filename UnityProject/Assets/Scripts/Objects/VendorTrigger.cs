using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class VendorTrigger : NetworkTabTrigger
{
	public List<VendorItem> VendorContent = new List<VendorItem>();
	public Color HullColor = Color.white;
	public bool EjectObjects = false;
	public EjectDirection EjectDirection = EjectDirection.None;
	[HideInInspector]
	public GameObject Originator;
	[HideInInspector]
	public Vector3 InteractPosition;
	[HideInInspector]
	public string InteractHand;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			Originator = originator;
			InteractPosition = position;
			InteractHand = hand;
		}
		return base.Interact(originator, position, hand);
	}
}

public enum EjectDirection { None, Up, Down, Random }

//Adding this as a separate class so we can easily extend it in future -
//add price or required access, stock amount and etc.
[System.Serializable]
public class VendorItem
{
	public GameObject Item;
	public int Stock = 5;

	public VendorItem(VendorItem item)
	{
		this.Item = item.Item;
		this.Stock = item.Stock;
	}
}