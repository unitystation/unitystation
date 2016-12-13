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

        void Start()
        {
        }

        void LateUpdate()
        {
            if (clothingItem == null && PlayerManager.control.LocalPlayer != null) //Wait until player is spawned before assigning ref
            {
                clothingItem = PlayerManager.control.LocalPlayer.transform.FindChild(clothingName).GetComponent<ClothingItem>();
            }

        }


        public void UpdateClothing(GameObject item)
        {
            clothingItem.UpdateReference(item.GetComponent<ItemAttributes>());
        }

        public void RemoveClothing()
        {
            clothingItem.Clear();
        }
    }
}
