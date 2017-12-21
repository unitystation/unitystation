using System.Collections;
using AccessType;
using Sprites;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;


// This manages access via ID cards. 
public class AccessRestrictions : MonoBehaviour
{
    public Access restriction;

    public bool CheckAccess(GameObject Player)
    {
        return CheckAccess(Player, gameObject);
    }

    public bool CheckAccess(GameObject Player, GameObject Object)
    {
        IDCard card;
        PlayerNetworkActions PNA = Player.GetComponent<PlayerNetworkActions>();

        // Check for an ID card
        if(PNA.Inventory["id"] != null &&
            PNA.Inventory["id"].GetComponent<IDCard>() != null)
        {
            card = PNA.Inventory["id"].GetComponent<IDCard>();
        }
        else if(PNA.Inventory[PNA.activeHand + "Hand"] != null &&
            PNA.Inventory[PNA.activeHand + "Hand"].GetComponent<IDCard>() != null)
        {
            card = PNA.Inventory[PNA.activeHand + "Hand"].GetComponent<IDCard>();
        }
        else
        {
            // If there isn't one, see if we even need one
            if ((int)restriction == 0)
            {
                return true;
            }
            // If there isn't one and we don't need one, we don't open the door
            return false;
        }

        // If we have an ID, make sure we have access
        if ((int)restriction == 0)
        {
            return true;
        }
        else if (card.accessSyncList.Contains((int)restriction))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}