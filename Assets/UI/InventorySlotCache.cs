using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// A pseudo-array/dictionary for retrieving inventory slots. 
    /// Supports multiple interactions to make it easier to access the slot you want to reference.
    /// </summary>
    /// <example>
    /// var beltSlot = inventorySlotCache.BeltSlot;
    /// </example>
    /// <example>
    /// var firstSlot = inventorySlotCache[0];
    /// </example>
    /// <example>
    /// var hatSlot = inventorySlotCache[ItemType.Hat];
    /// </example>
    /// <example>
    /// var idSlot = inventorySlotCache["id"];
    /// </example>
    /// <example>
    /// foreach (var slot in inventorySlotCache)
    /// </example>
    /// <example>
    /// inventorySlotCache.GetSlotByItem(CurrentSlot.Item)
    /// </example>
    public class InventorySlotCache : MonoBehaviour, IEnumerable<UI_ItemSlot>
    {
        public UI_ItemSlot BeltSlot;
        public UI_ItemSlot BackSlot;
        public UI_ItemSlot SuitStorageSlot;
        public UI_ItemSlot IDSlot;
        public UI_ItemSlot LeftPocketSlot;
        public UI_ItemSlot RightPocketSlot;
        public UI_ItemSlot RightHandSlot;
        public UI_ItemSlot LeftHandSlot;
        public UI_ItemSlot HeadSlot;
        public UI_ItemSlot SuitSlot;
        public UI_ItemSlot HandsSlot;
        public UI_ItemSlot ShoeSlot;
        public UI_ItemSlot UniformSlot;
        public UI_ItemSlot EarSlot;
        public UI_ItemSlot EyesSlot;
        public UI_ItemSlot NeckSlot;
        public UI_ItemSlot MaskSlot;

        private UI_ItemSlot[] slots;

        void Start()
        {
            slots = new[] {
                BackSlot,
                BeltSlot,
                EarSlot,
                EyesSlot,
                HandsSlot,
                HeadSlot,
                IDSlot,
                LeftHandSlot,
                LeftPocketSlot,
                MaskSlot,
                NeckSlot,
                RightHandSlot,
                RightPocketSlot,
                SuitSlot,
                SuitStorageSlot,
                ShoeSlot,
                UniformSlot,
            };
        }

        public UI_ItemSlot this[int index]
        {
            get
            {
                return slots[index];
            }
        }

        public UI_ItemSlot this[ItemType type]
        {
            get
            {
                return GetSlotByItemType(type);
            }
        }

        public UI_ItemSlot this[string eventName]
        {
            get
            {
                return GetSlotByEvent(eventName);
            }
        }

        IEnumerator<UI_ItemSlot> IEnumerable<UI_ItemSlot>.GetEnumerator()
        {
            return (slots as IEnumerable<UI_ItemSlot>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return slots.GetEnumerator();
        }

        public int Length
        {
            get
            {
                return slots != null ? slots.Length : 0;
            }
        }

        public ItemType GetItemType(GameObject obj)
        {
            var item = obj.GetComponent<ItemAttributes>();
            return item.type;
        }
        public SpriteType GetItemMasterType(GameObject obj)
        {
            var item = obj.GetComponent<ItemAttributes>();
            return item.spriteType;
        }

        /// <summary>
        /// Returns the most fitting slot for a given item to be equipped.
        /// </summary>
        /// <remarks>
        /// Returns the left pocket for non-equippable items.
        /// </remarks>
        public UI_ItemSlot GetSlotByItem(GameObject obj)
        {
            var item = obj.GetComponent<ItemAttributes>();
            return GetSlotByItemType(item.type);
        }

        public UI_ItemSlot GetSlotByItemType(ItemType type)
        {
            switch (type)
            {
                case ItemType.Back:
                    return BackSlot;
                case ItemType.Belt:
                    return BeltSlot;
                case ItemType.Ear:
                    return EarSlot;
                case ItemType.Glasses:
                    return EyesSlot;
                case ItemType.Gloves:
                    return HandsSlot;
                case ItemType.Hat:
                    return HeadSlot;
                case ItemType.ID:
                case ItemType.PDA:
                    return IDSlot;
                case ItemType.Mask:
                    return MaskSlot;
                case ItemType.Neck:
                    return NeckSlot;
                case ItemType.Shoes:
                    return ShoeSlot;
                case ItemType.Suit:
                    return SuitSlot;
                case ItemType.Uniform:
                    return UniformSlot;
                case ItemType.Gun:
                    return SuitStorageSlot;
                default:
                    return LeftPocketSlot;
            }
        }

        public UI_ItemSlot GetSlotByEvent(string eventName)
        {
            switch (eventName)
            {
                case "feet":
                case "shoes":
                    return ShoeSlot;
                case "uniform":
                    return UniformSlot;
                case "suit":
                    return SuitSlot;
                case "suitStorage":
                    return SuitStorageSlot;
                case "gloves":
                case "hands":
                    return HandsSlot;
                case "neck":
                    return NeckSlot;
                case "mask":
                    return MaskSlot;
                case "ear":
                case "ears":
                    return EarSlot;
                case "glasses":
                case "eyes":
                    return EyesSlot;
                case "hat":
                case "head":
                    return HeadSlot;
                case "id":
                case "pda":
                    return IDSlot;
                case "belt":
                    return BeltSlot;
                case "bag":
                case "back":
                    return BackSlot;
                case "rightHand":
                    return RightHandSlot;
                case "leftHand":
                    return LeftHandSlot;
                case "leftPocket":
                case "storage01":
                    return LeftPocketSlot;
                case "rightPocket":
                case "storage02":
                    return RightPocketSlot;
            }

            throw new InvalidOperationException("Unrecognized event name: " + eventName);
        }
    }
}