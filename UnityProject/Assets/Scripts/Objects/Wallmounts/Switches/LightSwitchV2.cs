using System;
using System.Collections;
using System.Collections.Generic;
using Electricity.Inheritance;
using Mirror;
using UnityEngine;
using Systems.Electricity;

namespace Objects.Lighting
{
	public class LightSwitchV2 : SubscriptionController, ICheckedInteractable<HandApply>, IAPCPowerable, ISetMultitoolMaster
	{
		public List<LightSource> listOfLights;

		public Action<bool> SwitchTriggerEvent;

		[SyncVar(hook = nameof(SyncState))]
		public bool isOn = true;

		[SerializeField]
		private float coolDownTime = 1.0f;

		private bool isInCoolDown;

		[SerializeField]
		private Sprite[] sprites = null;

		[SerializeField]
		private SpriteRenderer spriteRenderer = null;

		private PowerState powerState = PowerState.On;

		#region Lifecycle

		private void Awake()
		{
			foreach (var lightSource in listOfLights)
			{
				if (lightSource != null)
				{
					lightSource.SubscribeToSwitchEvent(this);
				}
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncState(isOn, isOn);
		}

		#endregion

		private void SyncState(bool oldState, bool newState)
		{
			isOn = newState;
			spriteRenderer.sprite = isOn ? sprites[0] : sprites[1];
		}

		[Server]
		public void ServerChangeState(bool newState, bool invokeEvent = true)
		{
			isOn = newState;
			if (invokeEvent == false) return;
			SwitchTriggerEvent?.Invoke(isOn);
		}

		#region ICheckedInteractable<HandApply>

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
			return !isInCoolDown;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			StartCoroutine(SwitchCoolDown());
			if (powerState == PowerState.Off || powerState == PowerState.LowVoltage) return;
			ServerChangeState(!isOn);
		}

		#endregion

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			if (isServer == false) return;
			switch (state)
			{
				case PowerState.OverVoltage:
				case PowerState.On:
					ServerChangeState(true, invokeEvent: false);
					powerState = state;
					break;
				case PowerState.LowVoltage:
				default:
					ServerChangeState(false, invokeEvent: false);
					powerState = state;
					break;
			}
		}

		#endregion

		private IEnumerator SwitchCoolDown()
		{
			isInCoolDown = true;
			yield return WaitFor.Seconds(coolDownTime);
			isInCoolDown = false;
		}

		#region ISetMultitoolMaster

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.LightSwitch;
		public MultitoolConnectionType ConType => conType;

		private bool multiMaster = true;
		public bool MultiMaster => multiMaster;

		public void AddSlave(object slaveObject) { }

		#endregion

		#region Editor

		private void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null) return;

			// Highlighting all controlled lightSources
			Gizmos.color = new Color(1, 1, 0, 1);
			for (int i = 0; i < listOfLights.Count; i++)
			{
				var lightSource = listOfLights[i];
				if (lightSource == null) continue;
				Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
				Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
			}
		}

		public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var lightSource = potentialObject.GetComponent<LightSource>();
				if (lightSource == null) continue;
				AddLightSourceFromScene(lightSource);
				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		private void AddLightSourceFromScene(LightSource lightSource)
		{
			if (listOfLights.Contains(lightSource))
			{
				listOfLights.Remove(lightSource);
				lightSource.relatedLightSwitch = null;
			}
			else
			{
				listOfLights.Add(lightSource);
				lightSource.relatedLightSwitch = this;
			}
		}

		#endregion
	}
}
