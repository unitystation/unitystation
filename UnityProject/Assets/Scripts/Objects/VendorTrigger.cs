using PlayGroups.Input;
using UI;
using UnityEngine;
using System.Collections.Generic;
using Crafting;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;


public class VendorTrigger : InputTrigger
{
	[Header("Vendor content")] public GameObject[] vendorcontent;
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		Debug.Log("spewing out: ");
		if (!isServer)
		{
			foreach (GameObject item in vendorcontent)
			{
				Debug.Log("Server client: " + item);
				VendorMessage.Send(item, gameObject);
			}
		}
		else
		{

			foreach (GameObject item in vendorcontent)
			{
				Debug.Log("Server item: " + item);
				ItemFactory.SpawnItem(item, transform.position, transform.parent);
			}
		}
		
	}
	
}
