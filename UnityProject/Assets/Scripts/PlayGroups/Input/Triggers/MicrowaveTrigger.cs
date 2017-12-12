using InputControl;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using Equipment;


public class MicrowaveTrigger : InputTrigger
{
    private Microwave microwave;
    void Start()
    {
        microwave = GetComponent<Microwave>();
    }

    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        if (!isServer)
        {
            var slot = UIManager.Hands.CurrentSlot;

            // Client pre-approval
            if (!microwave.Cooking && slot.CanPlaceItem())
            {
                //Client informs server of interaction attempt
                InteractMessage.Send(gameObject, position, slot.eventName);
                //Client simulation
                //              
                //              if ( !waveOk )
                //              {
                //                  Debug.Log("Client placing error");
                //              }
            }
        }
        else
        {   //Server actions
            if (!ValidateMicrowaveInteraction(originator, position, hand))
            {
                //Rollback prediction here
                //              originator.GetComponent<PlayerNetworkActions>().RollbackPrediction(hand);           
            }
        }
    }

    [Server]
    private bool ValidateMicrowaveInteraction(GameObject originator, Vector3 position, string hand)
    {
        var ps = originator.GetComponent<PlayerScript>();
        if (ps.canNotInteract() || !ps.IsInReach(position))
        {
            return false;
        }

        GameObject item = ps.playerNetworkActions.Inventory[hand];
        if (item == null) return false;
        var attr = item.GetComponent<ItemAttributes>();

        var ingredient = new Ingredient(attr.itemName);

        var meal = CraftingManager.Meals.FindRecipe(new List<Ingredient>() { ingredient });

        if (meal)
        {
            ps.playerNetworkActions.CmdStartMicrowave(hand, gameObject, meal.name);
            item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
        }


        return true;
    }


}