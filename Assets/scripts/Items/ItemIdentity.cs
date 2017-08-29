using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ItemIdentity : NetworkBehaviour
{
    // see cards_id.dm

    public int MiningPoints = 0; //For redeeming at mining equipment vendors
    public List<string> Access = new List<string>();
    public string RegisteredName;
    public string Assignment;
    
    // Use this for initialization
    void Start () {
        Access.Add("Test");
	}
	
	// Update is called once per frame
	void Update () {
    }

    public void OnExamine()
    {
        UI.UIManager.Chat.AddChatEvent(new ChatEvent("This is " + RegisteredName + "'s ID card\nThey are the " + Assignment + " of the station!"));
        if (MiningPoints > 0)
        {
            UI.UIManager.Chat.AddChatEvent(new ChatEvent("There's " + MiningPoints + " mining equipment redemption points loaded onto this card."));
        }
    }
}
