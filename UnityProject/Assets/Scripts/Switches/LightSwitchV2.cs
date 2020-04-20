using System;
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
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (powerState == PowerStates.Off) return;
			ServerChangeState(!isOn);
			Debug.Log("Switch Pressed");
		}

		private void SyncState(bool oldState, bool newState)
		{
			isOn = newState;
		}

		[Server]
		public void ServerChangeState(bool newState)
		{
			isOn = newState;
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
			switch (State)
			{
				case PowerStates.On:
					ServerChangeState(true);
					powerState = State;
					break;
				case PowerStates.LowVoltage:
					break;
				case PowerStates.OverVoltage:
					break;
				default:
					ServerChangeState(false);
					powerState = State;
					break;
			}
		}
	}
}