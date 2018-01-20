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
using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UnityEngine;


public class VendorTrigger : InputTrigger
{
	public GameObject[] vendorcontent;

	public bool allowSell = true;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		// Client pre-approval
		if (!isServer)
		{
			UI_ItemSlot slot = UIManager.Hands.CurrentSlot;
			UIManager.Chat.AddChatEvent(new ChatEvent("bzzzz... ", ChatChannel.Examine));
			//Client informs server of interaction attempt
			InteractMessage.Send(gameObject, position, slot.eventName);

		}
		else
		{

			UIManager.Chat.AddChatEvent(new ChatEvent("bzzzz... ", ChatChannel.Examine));
			ValidateVendorInteraction(originator, position, hand);
			

		}
		
	}
	
	[Server]
	private bool ValidateVendorInteraction(GameObject originator, Vector3 position, string hand)
	{
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (ps.canNotInteract() || !ps.IsInReach(position) || !allowSell)
		{
			return false;
		}
		allowSell = false;
		foreach (GameObject item in vendorcontent)
		{

			ItemFactory.SpawnItem(item, transform.position, transform.parent);
		}
		StartCoroutine(VendorInputCoolDown());
		allowSell = true;
		return true;
	}
	
	private IEnumerator VendorInputCoolDown()
	{
		yield return new WaitForSeconds(2f);
		
	}
	
}
