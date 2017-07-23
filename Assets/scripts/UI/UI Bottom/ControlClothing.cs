using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        public GameObject equipmentMenu;

		private bool isOpen;
		private RectTransform rect;

        void Start() {
			isOpen = false;
			rect = equipmentMenu.GetComponent<RectTransform>();
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
				Vector2 pos = rect.anchoredPosition;
				pos.x = -486f;
				rect.anchoredPosition = pos;
			} else {
				Vector2 pos = rect.anchoredPosition;
				pos.x = -1486f;
				rect.anchoredPosition = pos;
			}
		}
    }
}