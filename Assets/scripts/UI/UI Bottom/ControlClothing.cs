using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        public GameObject equipmentMenu;
		private bool isOpen;
		private Vector3 cacheLocalPos;

        void Start() {
			isOpen = false;
			cacheLocalPos = equipmentMenu.transform.localPosition;
			ToggleEquipMenu(false);
        }

        public void RolloutEquipmentMenu() {
            SoundManager.Play("Click01");

            if(isOpen) {
				ToggleEquipMenu(false);
            } else {
				ToggleEquipMenu(true);
            }
        }

		private void ToggleEquipMenu(bool isOn){
			isOpen = isOn;
			if (isOn) {
				equipmentMenu.transform.localPosition = cacheLocalPos;
			} else {
				Vector3 newPos = cacheLocalPos;
				newPos.x -= 1000f;
				equipmentMenu.transform.localPosition = newPos;
			}
		}
    }
}