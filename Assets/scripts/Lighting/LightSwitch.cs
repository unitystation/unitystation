using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

namespace Lighting
{
	public class LightSwitch : Photon.PunBehaviour
	{
		public bool isOn = true;
		private SpriteRenderer thisSprite;
		public Sprite lightOn;
		public Sprite lightOff;
		private LightingRoom lightingRoomParent;
		private bool switchCoolDown = false;
		private AudioSource clickSFX;

		void Awake()
		{
			thisSprite = GetComponentInChildren<SpriteRenderer>();
			lightingRoomParent = transform.parent.gameObject.GetComponentInParent<LightingRoom>();
			clickSFX = GetComponent<AudioSource>();
		}

		void OnMouseDown()
		{
			if (PlayerManager.PlayerInReach(transform)) {
				if (!switchCoolDown) {
					switchCoolDown = true;
					StartCoroutine(CoolDown());
					if (isOn) {
						isOn = false;
						photonView.RPC("SwitchOff", PhotonTargets.All, null);
					} else {
						isOn = true;
						photonView.RPC("SwitchOn", PhotonTargets.All, null);
					}
				}
			}
		}

		IEnumerator CoolDown(){
			yield return new WaitForSeconds(0.2f);
			switchCoolDown = false;
		}

		[PunRPC]
		public void SwitchOn()
		{
			thisSprite.sprite = lightOn;
			clickSFX.Play();
			lightingRoomParent.LightSwitchOn();
		}

		[PunRPC]
		public void SwitchOff()
		{
			thisSprite.sprite = lightOff;
			clickSFX.Play();
			lightingRoomParent.LightSwitchOff();
		}

	}
}
