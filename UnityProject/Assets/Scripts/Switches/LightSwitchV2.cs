using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Lighting
{
	public class LightSwitchV2 : SwitchBase, ICheckedInteractable<HandApply>
	{
		public Action<bool> switchTriggerEvent;

		[SyncVar(hook = nameof(SyncState))]
		public bool isOn = true;

		private void Awake()
		{
			foreach (var lightSource in listOfTriggers)
			{
				var light = lightSource as LightSource;
				if(light != null)
					light.SubscribeToSwitch(ref switchTriggerEvent);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
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
			for (int i = 0; i < listOfTriggers.Count; i++)
			{
				var lightSource = listOfTriggers[i];
				if(lightSource == null) continue;
				Gizmos.DrawLine(sprite.transform.position, lightSource.transform.position);
				Gizmos.DrawSphere(lightSource.transform.position, 0.25f);
			}
		}

	}
}