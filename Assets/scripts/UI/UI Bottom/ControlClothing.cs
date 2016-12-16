using UnityEngine;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        public GameObject equipmentMenu;

        void Start() {
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