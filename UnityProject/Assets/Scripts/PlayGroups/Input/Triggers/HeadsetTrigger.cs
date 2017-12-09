using Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using Items;

namespace InputControl
{
    public class ScrewdriverTrigger : PickUpTrigger
    {

        private Headset headset;

        void Start()
        {
            headset = GetComponent<Headset>();
        }

        public override void Interact(GameObject originator, string hand)
        {
			Debug.Log("in headset interact");
            var item = UIManager.Hands.CurrentSlot.Item;

            if (item && item.GetComponent<Scewdriver>()) {
				RemoveEncryptionKeyMessage.Send(gameObject);
            }
        }
    }
}