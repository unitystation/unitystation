﻿using System.Collections.Generic;
using Events;
using PlayGroup;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
	public class UI_ItemSlot : MonoBehaviour
	{
		public bool allowAllItems;
		public List<ItemType> allowedItemTypes;
		public string eventName;

		private Image image;
		public ItemSize maxItemSize;

		public GameObject Item { get; private set; }

		public bool IsFull => Item != null;

		private void Awake()
		{
			image = GetComponent<Image>();
            image.alphaHitTestMinimumThreshold = 0.5f;
            image.enabled = false;
			if (eventName.Length > 0)
			{
				//                Debug.LogErrorFormat("Triggered SetItem for {0}",slotName);
				EventManager.UI.AddListener(eventName, SetItem);
			}
		}

		private void OnEnable()
		{
			SceneManager.sceneLoaded += OnLevelFinishedLoading;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		}

		//Reset Item slot sprite on game restart
		private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
		{
			image.sprite = null;
			image.enabled = false;
		}

		/// <summary>
		///     direct low-level method, doesn't send anything to server
		/// </summary>
		public void SetItem(GameObject item)
		{
			if (!item)
			{
				Clear();
				return;
			}
			//            var itemAttributes = item.GetComponent<ItemAttributes>();
			//            Debug.LogFormat("Setting item {0}/{1} to {2}", item.name, itemAttributes ? itemAttributes.itemName : "(no iAttr)", eventName);
			image.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
			image.enabled = true;
			Item = item;
			item.transform.position = transform.position;
		}

		//        public bool TrySetItem(GameObject item) {
		//            if(!IsFull && item != null && CheckItemFit(item)) {
		////                Debug.LogErrorFormat("TrySetItem TRUE for {0}", item.GetComponent<ItemAttributes>().hierarchy);
		//                InventoryInteractMessage.Send(eventName, item, true);
		//               //predictions:
		//                UIManager.UpdateSlot(new UISlotObject(eventName, item));
		////                SetItem(item);
		//
		//                return true;
		//            }
		////            Debug.LogErrorFormat("TrySetItem FALSE for {0}", item.GetComponent<ItemAttributes>().hierarchy);
		//            return false;
		//        }

		/// <summary>
		///     removes item from slot
		/// </summary>
		/// <returns></returns>
		public GameObject Clear()
		{
			PlayerScript lps = PlayerManager.LocalPlayerScript;
			if (!lps || lps.canNotInteract())
			{
				return null;
			}

			GameObject item = Item;
			//            InputTrigger.Touch(Item);
			Item = null;
			image.sprite = null;
			image.enabled = false;

			return item;
		}

		/// <summary>
		///     Clientside check for dropping/placing objects from inventory slot
		/// </summary>
		public bool CanPlaceItem()
		{
			return IsFull && UIManager.SendUpdateAllowed(Item);
		}

		/// <summary>
		///     clientside simulation of placement
		/// </summary>
		public bool PlaceItem(Vector3 pos)
		{
			var item = Clear();
			if (!item)
			{
				return false;
			}
			var itemTransform = item.GetComponent<CustomNetTransform>();
			itemTransform.AppearAtPosition(pos);
			var itemAttributes = item.GetComponent<ItemAttributes>();
			Debug.LogFormat("Placing item {0}/{1} from {2} to {3}", item.name, itemAttributes ? itemAttributes.itemName : "(no iAttr)", eventName, pos);
			return true;
		}

		public void Reset()
		{
			image.sprite = null;
			image.enabled = false;
			Item = null;
		}

		public bool CheckItemFit(GameObject item)
		{
			ItemAttributes attributes = item.GetComponent<ItemAttributes>();

			if (!allowAllItems)
			{
				if (!allowedItemTypes.Contains(attributes.type))
				{
					return false;
				}
			}
			else if ( attributes.size > maxItemSize )
			{
				Logger.Log($"{attributes.size} {item} is too big for {maxItemSize} {eventName}!");
				return false;
			}
			return allowAllItems || allowedItemTypes.Contains(attributes.type);
		}
	}
}