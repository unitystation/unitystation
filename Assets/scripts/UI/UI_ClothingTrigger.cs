using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{

    public class UI_ClothingTrigger: MonoBehaviour
    {

        public string clothingName;

        private ClothingItem clothingItem;

        private ItemAttributes attributesToSet;

        void Start()
        {
        }

        void LateUpdate()
        {
            if (clothingItem == null && PlayerManager.control.LocalPlayer != null) //Wait until player is spawned before assigning ref
            {
                clothingItem = PlayerManager.control.LocalPlayer.transform.FindChild(clothingName).GetComponent<ClothingItem>();

                if(attributesToSet != null)
                    clothingItem.UpdateReference(attributesToSet);
            }

        }


        public void UpdateClothing(GameObject item)
        {
            var attributes = item.GetComponent<ItemAttributes>();
            
            if(clothingItem == null)
                attributesToSet = attributes;
            else
                clothingItem.UpdateReference(attributes);
        }

        public void RemoveClothing()
        {
            clothingItem.Clear();
        }
    }
}
