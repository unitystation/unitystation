using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class VendorTrigger : InputTrigger
{
	public GameObject[] vendorcontent;

    // A dictionary to map stock items to stock values
    public Dictionary<GameObject, int> stockAmts = new Dictionary<GameObject, int>();

    // A boolean to see if we've initialized correctly, if not, we need to map our dictionary
    bool hasInit = false;

    public bool allowSell = true;
	public float cooldownTimer = 2f;
	public int stock = 5;
	public string interactionMessage;
	public string deniedMessage;

    // These fields are required for our little "interact detour"
    // Essentially, Interact -> Window -> Vend
    public GameObject originator;
    public Vector3 position;
    public string hand;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Init();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Init();
    }

    // If we haven't already mapped our dictionary, fill it up with 5 of each item
    public void Init()
    {
        if (!hasInit)
        {
            foreach(GameObject stockItem in vendorcontent)
            {
                stockAmts.Add(stockItem, stock);
            }

            hasInit = true;
        }
    }


    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        this.originator = originator;
        this.position = position;
        this.hand = hand;

        // If the vending window is already open, don't open another
        if (UIManager.Display.vendingWindow.activeSelf)
        {
            return;
        }

        // Set the vending window as active and show it
        UIManager.Display.vendingWindow.SetActive(true);
        GUI_Vending vendingWindow = UIManager.Display.vendingWindow.GetComponent<GUI_Vending>();
        vendingWindow.OpenWindow(this);
    }

    public void Vend(GameObject itemToVend)
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

			ServerVendorInteraction(originator, position, hand, itemToVend);
			StartCoroutine(VendorInputCoolDown());
		}

	}

    [Server]
	private bool ServerVendorInteraction(GameObject originator, Vector3 position, string hand, GameObject itemToVend)
	{
//		Debug.Log("status" + allowSell);
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (ps.canNotInteract() || !ps.IsInReach(position) || vendorcontent.Length == 0)
		{
			return false;
		}

		ItemFactory.SpawnItem(itemToVend, transform.position, transform.parent);

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
