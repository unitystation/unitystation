using UnityEngine;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        private GameObject equipmentMenu;

        void Start() {
            Transform equipmentMenuTransform = transform.FindChild("EquipmentMenu");
            equipmentMenu = equipmentMenuTransform.gameObject;
            equipmentMenu.SetActive(false);
        }

        public void RolloutEquipmentMenu() {
            SoundManager.control.Play("Click01");

            if(!equipmentMenu.GetActive()) {
                equipmentMenu.SetActive(true);
            } else {
                equipmentMenu.SetActive(false);
            }
        }
    }
}