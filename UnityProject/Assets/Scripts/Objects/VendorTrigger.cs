using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class VendorTrigger : InputTrigger
{
	public GameObject[] vendorcontent;

	public bool allowSell = true;
	public float cooldownTimer = 2f;
	public int stock = 5;
	public string interactionMessage;
	public string deniedMessage;

	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!allowSell && deniedMessage != null && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UIManager.Chat.AddChatEvent(new ChatEvent(deniedMessage, ChatChannel.Examine));
		}
		// Client pre-approval
		else if (!isServer && allowSell)
		{
			allowSell = false;
			UI_ItemSlot slot = UIManager.Hands.CurrentSlot;
			UIManager.Chat.AddChatEvent(new ChatEvent(interactionMessage, ChatChannel.Examine));
			//Client informs server of interaction attempt
			InteractMessage.Send(gameObject, position, slot.eventName);
			StartCoroutine(VendorInputCoolDown());
		}
		else if(allowSell)
		{
			allowSell = false;
			if (!GameData.Instance.testServer && !GameData.IsHeadlessServer)
			{
				UIManager.Chat.AddChatEvent(new ChatEvent(interactionMessage, ChatChannel.Examine));
			}

			ServerVendorInteraction(originator, position, hand);
			StartCoroutine(VendorInputCoolDown());
		}

	}

	[Server]
	private bool ServerVendorInteraction(GameObject originator, Vector3 position, string hand)
	{
//		Debug.Log("status" + allowSell);
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (ps.canNotInteract() || !ps.IsInReach(position) || vendorcontent.Length == 0)
		{
			return false;
		}

		int randIndex = Random.Range(0, vendorcontent.Length);
		ItemFactory.SpawnItem(vendorcontent[randIndex], transform.position, transform.parent);

		stock--;

		return true;
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return new WaitForSeconds(cooldownTimer);
		if ( stock > 0 )
		{
			allowSell = true;
		}
	}
	
}
