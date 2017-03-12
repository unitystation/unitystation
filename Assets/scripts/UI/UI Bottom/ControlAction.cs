﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PlayGroup;


namespace UI {
    public class ControlAction: MonoBehaviour {

        public Sprite[] throwSprites;
        public Image throwImage;

        void Start() {
            UIManager.IsThrow = false;
        }

        /* 
		 * Button OnClick methods
		 */

        void Update() {
            CheckKeyboardInput();
        }

        void CheckKeyboardInput() {
            if(Input.GetKeyDown(KeyCode.Q)) {
                Drop();
            }
        }

        public void Resist() {
            SoundManager.Play("Click01");
            Debug.Log("Resist Button");
        }

        public void Drop() {
            SoundManager.Play("Click01");
            Debug.Log("Drop Button");
            GameObject item = UIManager.Hands.CurrentSlot.Clear();
            item.transform.position = PlayerManager.LocalPlayer.transform.position;
            item.transform.parent = null;
			item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

        }

        public void Throw() {
            SoundManager.Play("Click01");
            Debug.Log("Throw Button");

            if(!UIManager.IsThrow) {
                UIManager.IsThrow = true;
                throwImage.sprite = throwSprites[1];

            } else {
                UIManager.IsThrow = false;
                throwImage.sprite = throwSprites[0];
            }
        }
    }
}