using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Systems.Electricity;
using Systems.Interaction;
using CustomInspectors;
using Shared.Systems.ObjectConnection;


namespace Objects.Lighting
{
	public class LightSwitchV2 : ImnterfaceMultitoolGUI, ISubscriptionController, ICheckedInteractable<HandApply>, IAPCPowerable, IMultitoolMasterable, ICheckedInteractable<AiActivate>
	{
		public List<LightSource> listOfLights;

		[NonSerialized] public Action<bool> SwitchTriggerEvent;

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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;

			if (side == NetworkSide.Server)
			{
				if (isInCoolDown) return false;
				StartCoroutine(SwitchCoolDown());
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			TryInteraction();
		}

		#endregion

		private void TryInteraction()
		{
			if (powerState == PowerState.Off || powerState == PowerState.LowVoltage) return;
			ServerChangeState(!isOn);
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			if (isInCoolDown) return false;

			//Trigger client cooldown only, or else it will break for local host
			if (CustomNetworkManager.IsServer == false)
			{
				StartCoroutine(SwitchCoolDown());
			}

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//Start server cooldown
			StartCoroutine(SwitchCoolDown());
			TryInteraction();
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

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.LightSwitch;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		#region Editor

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
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
