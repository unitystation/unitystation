using UnityEngine;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        private GameObject equipmentMenu;

        private UI_ItemSlot shoesSlot;
        private UI_ItemSlot suitSlot;
        private UI_ItemSlot armorSlot;
        private UI_ItemSlot glovesSlot;
        private UI_ItemSlot neckSlot;
        private UI_ItemSlot maskSlot;
        private UI_ItemSlot earSlot;
        private UI_ItemSlot glassesSlot;
        private UI_ItemSlot hatSlot;

        void Start() {
            Transform equipmentMenuTransform = transform.FindChild("EquipmentMenu");
            equipmentMenu = equipmentMenuTransform.gameObject;
            equipmentMenu.SetActive(false);

            shoesSlot = equipmentMenuTransform.FindChild("Shoes").GetComponentInChildren<UI_ItemSlot>();
            suitSlot = equipmentMenuTransform.FindChild("Suit").GetComponentInChildren<UI_ItemSlot>();
            armorSlot = equipmentMenuTransform.FindChild("Armor").GetComponentInChildren<UI_ItemSlot>();
            glovesSlot = equipmentMenuTransform.FindChild("Gloves").GetComponentInChildren<UI_ItemSlot>();
            neckSlot = equipmentMenuTransform.FindChild("Neck").GetComponentInChildren<UI_ItemSlot>();
            maskSlot = equipmentMenuTransform.FindChild("Mask").GetComponentInChildren<UI_ItemSlot>();
            earSlot = equipmentMenuTransform.FindChild("Ear").GetComponentInChildren<UI_ItemSlot>();
            glassesSlot = equipmentMenuTransform.FindChild("Glasses").GetComponentInChildren<UI_ItemSlot>();
            hatSlot = equipmentMenuTransform.FindChild("Hat").GetComponentInChildren<UI_ItemSlot>();
        }

        public void RolloutEquipmentMenu() {
            PlayClick01();
            if(!equipmentMenu.GetActive()) {
                equipmentMenu.SetActive(true);
            } else {
                equipmentMenu.SetActive(false);
            }
        }

        public void Shoes() {
            PlayClick01();
            shoesSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Suit() {
            PlayClick01();
            suitSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Armor() {
            PlayClick01();
            armorSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Gloves() {
            PlayClick01();
            glovesSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Neck() {
            PlayClick01();
            neckSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Mask() {
            PlayClick01();
            maskSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Ear() {
            PlayClick01();
            earSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Glasses() {
            PlayClick01();
            glassesSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        public void Hat() {
            PlayClick01();
            hatSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }

        void PlayClick01() {
            if(SoundManager.control != null) {
                SoundManager.control.Play("Click01");
            }
        }
    }
}