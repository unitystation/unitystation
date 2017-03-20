using UnityEngine;
using System.Collections;
using PlayGroup;
using InputControl;
using System;

namespace Items {
    public class PickUpTrigger: InputTrigger {

        public override void Interact() {
            // try to add the item to hand
            ItemManager.TryToPickUpObject(gameObject);
        }
    }
}
