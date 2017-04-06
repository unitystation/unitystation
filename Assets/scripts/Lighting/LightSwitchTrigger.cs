using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using InputControl;

namespace Lighting
{
	public class LightSwitchTrigger: InputTrigger
	{
        [SyncVar(hook = "SyncLightSwitch")]
		public bool isOn = true;
		private SpriteRenderer spriteRenderer;
		public Sprite lightOn;
		public Sprite lightOff;
		private bool switchCoolDown = false;
		private AudioSource clickSFX;

		void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			clickSFX = GetComponent<AudioSource>();
		}

		public override void Interact()
		{
			if (!switchCoolDown) {
				switchCoolDown = true;
				StartCoroutine(CoolDown());
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleLightSwitch(gameObject);
			}
		}

		IEnumerator CoolDown()
		{
			yield return new WaitForSeconds(0.2f);
			switchCoolDown = false;
		}
          
		void SyncLightSwitch(bool _on)
		{
			if (!_on) {
				spriteRenderer.sprite = lightOff;
				clickSFX.Play();
			} else {
				spriteRenderer.sprite = lightOn;
				clickSFX.Play();
			}
		}
	}
}
