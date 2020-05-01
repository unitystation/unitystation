﻿using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lighting
{
	public class LightSwitchV2 : NetworkBehaviour, ICheckedInteractable<HandApply>,IAPCPowered
	{
		public List<LightSource> listOfLights;

		public Action<bool> switchTriggerEvent;

		[SyncVar(hook = nameof(SyncState))]
		public bool isOn = true;

		[SerializeField]
		private float coolDownTime = 1.0f;

		private bool isInCoolDown;

		[SerializeField]
		private Sprite[] sprites = null;

		[SerializeField]
		private SpriteRenderer spriteRenderer = null;

		private PowerStates powerState = PowerStates.On;
		private void Awake()
		{
			foreach (var lightSource in listOfLights)
			{
				if(lightSource != null)
					lightSource.SubscribeToSwitchEvent(this);
			}
		}

		public override void OnStartClient()
		{
			SyncState(isOn, isOn);
			base.OnStartClient();
		}
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
			return !isInCoolDown;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (!isServer) 
			{
				Logger.Log("Client tried to run ServerPerformInteraction() in LightSwitchV2.cs. Tsk Tsk.");
				return;
			}
			StartCoroutine(SwitchCoolDown());
			if (powerState == PowerStates.Off || powerState == PowerStates.LowVoltage) return;
			ServerChangeState(!isOn);
		}

		private void SyncState(bool oldState, bool newState)
		{
			isOn = newState;
			spriteRenderer.sprite = isOn ? sprites[0] : sprites[1];
		}

		[Server]
		public void ServerChangeState(bool newState, bool invokeEvent = true)
		{
			isOn = newState;
			if (!invokeEvent) return;
			switchTriggerEvent?.Invoke(isOn);
		}

		void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;

			//Highlighting all controlled lightSources
			Gizmos.color = new Color(1, 1, 0, 1);
			for (int i = 0; i < listOfLights.Count; i++)
			{
				var lightSource = listOfLights[i];
				if(lightSource == null) continue;
				Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
				Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
			}
		}

		public void PowerNetworkUpdate(float Voltage)
		{

		}

		public void StateUpdate(PowerStates State)
		{
			if (!isServer) 
			{
				Logger.Log("Client tried to run StateUpdate() in LightSwitchV2.cs. Tsk Tsk");
				return;
			}
			switch (State)
			{
				case PowerStates.On:
					ServerChangeState(true,invokeEvent:false);
					powerState = State;
					break;
				case PowerStates.LowVoltage:
					ServerChangeState(false,invokeEvent:false);
					powerState = State;
					break;
				case PowerStates.OverVoltage:
					ServerChangeState(true,invokeEvent:false);
					powerState = State;
					break;
				default:
					ServerChangeState(false,invokeEvent:false);
					powerState = State;
					break;
			}
		}

		private IEnumerator SwitchCoolDown()
		{
			isInCoolDown = true;
			yield return WaitFor.Seconds(coolDownTime);
			isInCoolDown = false;
		}
	}
}