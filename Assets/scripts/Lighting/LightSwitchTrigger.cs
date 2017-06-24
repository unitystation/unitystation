﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using InputControl;

namespace Lighting
{
	public class LightSwitchTrigger: InputTrigger
	{
		public ObjectTrigger[] TriggeringObjects;

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

		public override void OnStartClient()
		{
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

		void SyncLightSwitch(bool _on)
		{
			isOn = _on;
			foreach (var s in TriggeringObjects) {
				s.Trigger(_on);
			}

			clickSFX.Play();
			spriteRenderer.sprite = _on ? lightOn : lightOff;
		}
	}
}
