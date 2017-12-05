using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Events;
using UnityEngine.Events;

namespace PlayGroup
{
    public class ItemChanged : MonoBehaviour
    {

        public string eventName;

        private ClothingItem clothingItem;
        private PlayerScript playerScript;

        void Start()
        {
            clothingItem = GetComponent<ClothingItem>();
            playerScript = GetComponentInParent<PlayerScript>();
            EventManager.UI.AddListener(eventName, new UnityAction<GameObject>(OnChanged));
        }

        void OnChanged(GameObject item)
        {
            if (playerScript.isLocalPlayer)
            { //Only change the one that is mine
                ChangeItem(item);
            }
            else
            { //Dev mode
                ChangeItem(item);
            }
        }

        void ChangeItem(GameObject item)
        {
            if (item)
            {

            }
            else
            {
                clothingItem.Clear();
            }

        }
    }
}
