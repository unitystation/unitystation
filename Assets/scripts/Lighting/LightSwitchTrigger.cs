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
		public List<ObjectTrigger> TriggeringObjects = new List<ObjectTrigger>();

		[SyncVar(hook = "SyncLightSwitch")]
		public bool isOn = true;
		private SpriteRenderer spriteRenderer;
		public Sprite lightOn;
		public Sprite lightOff;
		private bool switchCoolDown = false;
		private AudioSource clickSFX;

		public ObjectTrigger lightSprite;

		void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			clickSFX = GetComponent<AudioSource>();
			TriggeringObjects.Clear();
			foreach (Transform t in transform) {
				var o = t.GetComponent<ObjectTrigger>();
				if(0 != null)
				TriggeringObjects.Add(o);
			}
		}

	 	public override void OnStartClient()
		{
            StartCoroutine(WaitForLoad());
		}

        IEnumerator WaitForLoad(){
            yield return new WaitForSeconds(0.2f);
            SyncLightSwitch(isOn);
        }

		public override void Interact()
		{
			if (!PlayerManager.LocalPlayerScript.IsInReach(spriteRenderer.transform, 1f))
				return;

			if (switchCoolDown)
				return;
			
			StartCoroutine(CoolDown());
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleLightSwitch(gameObject);
		}

		IEnumerator CoolDown()
		{
			switchCoolDown = true;
			yield return new WaitForSeconds(0.2f);
			switchCoolDown = false;
		}

		void SyncLightSwitch(bool state)
		{
			if (lightSprite != null) {
				lightSprite.Trigger(state);
			}
			if (TriggeringObjects != null) {
				foreach (var s in TriggeringObjects) {
					if (s != null) {
						s.Trigger(state);
					}
				}
			}

			if (clickSFX != null) {
				clickSFX.Play();
			}

			if (spriteRenderer != null) {
				spriteRenderer.sprite = state ? lightOn : lightOff;
			}
		}
	}
}
