using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;

namespace PlayGroup
{
    public class ItemChanged: MonoBehaviour
    {

        public string eventName;

        private ClothingItem clothingItem;

        void Start()
        {
            clothingItem = GetComponent<ClothingItem>();

            EventManager.UI.AddListener(eventName, new UnityAction<GameObject>(OnChanged));
        }

        void OnChanged(GameObject item)
        {
            if (PhotonNetwork.connectedAndReady) //connected
            {
                if (clothingItem.photonView.isMine) //Only change the one that is mine
                {
                    ChangeItem(item);
                }
            }
            else //Dev mode
            {
                ChangeItem(item);
            }
        }

        void ChangeItem(GameObject item)
        {
            if (item)
            {
                clothingItem.UpdateItem(item);
            }
            else
            {
                clothingItem.Clear();
            }

        }
    }
}
