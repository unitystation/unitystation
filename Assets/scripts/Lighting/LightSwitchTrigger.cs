using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using InputControl;

namespace Lighting {
    public class LightSwitchTrigger: InputTrigger {
        public bool isOn = true;
        private SpriteRenderer spriteRenderer;
        public Sprite lightOn;
        public Sprite lightOff;
        private bool switchCoolDown = false;
        private AudioSource clickSFX;

        void Awake() {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            clickSFX = GetComponent<AudioSource>();
        }

        public override void Interact() {
            if(!switchCoolDown) {
                switchCoolDown = true;
                StartCoroutine(CoolDown());
                if(isOn) {
                    isOn = false;
                    photonView.RPC("SwitchOff", PhotonTargets.All, null);
                } else {
                    isOn = true;
                    photonView.RPC("SwitchOn", PhotonTargets.All, null);
                }
            }
        }

        IEnumerator CoolDown() {
            yield return new WaitForSeconds(0.2f);
            switchCoolDown = false;
        }

        [PunRPC]
        public void SwitchOn() {
            spriteRenderer.sprite = lightOn;
            clickSFX.Play();
        }

        [PunRPC]
        public void SwitchOff() {
            spriteRenderer.sprite = lightOff;
            clickSFX.Play();
        }

    }
}
