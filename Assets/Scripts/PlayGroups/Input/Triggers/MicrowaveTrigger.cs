using Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

namespace InputControl
{
    public class MicrowaveTrigger : InputTrigger
    {

        private Microwave microwave;

        void Start()
        {
            microwave = GetComponent<Microwave>();
        }

        public override void Interact(GameObject originator, string hand)
        {
            var item = UIManager.Hands.CurrentSlot.Item;

            if (!microwave.Cooking && item)
            {
                var attr = item.GetComponent<ItemAttributes>();

                var ingredient = new Ingredient(attr.itemName);

                var meal = CraftingManager.Meals.FindRecipe(new List<Ingredient>() { ingredient });

                if (meal)
                {
                    UIManager.Hands.CurrentSlot.Clear();
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStartMicrowave(gameObject, meal.name);
                }
            }
        }
    }
}