using UnityEngine;
using System.Collections;

namespace UI {
    public class ControlClothing: MonoBehaviour {
        private GameObject equipmentMenu;
        
        void Start() {
            equipmentMenu = transform.FindChild("EquipmentMenu").gameObject;
            equipmentMenu.SetActive(false);
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
            Debug.Log("Shoes Button");

        }

        public void Suit() {
            PlayClick01();
            Debug.Log("Suit Button");

        }

        public void Armor() {
            PlayClick01();
            Debug.Log("Armor Button");

        }

        public void Gloves() {
            PlayClick01();
            Debug.Log("Gloves Button");

        }

        public void Neck() {
            PlayClick01();
            Debug.Log("Neck Button");

        }

        public void Mask() {
            PlayClick01();
            Debug.Log("Mask Button");

        }

        public void Ear() {
            PlayClick01();
            Debug.Log("Ear Button");

        }

        public void Glasses() {
            PlayClick01();
            Debug.Log("Glasses Button");

        }

        public void Hat() {
            PlayClick01();
            Debug.Log("Hat Button");

        }

        void PlayClick01() {
            if(SoundManager.control != null) {
                SoundManager.control.sounds["Click01"].Play();
            }
        }
    }
}