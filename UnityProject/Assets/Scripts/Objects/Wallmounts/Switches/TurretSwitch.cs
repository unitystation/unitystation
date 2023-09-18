using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Messages.Server;
using Systems.Electricity;
using Systems.Interaction;
using CustomInspectors;
using Logs;
using UI.Core.Net;
using Objects.Other;
using Shared.Systems.ObjectConnection;


namespace Objects.Wallmounts.Switches
{
	[RequireComponent(typeof(AccessRestrictions))]
	[RequireComponent(typeof(APCPoweredDevice))]
	public class TurretSwitch : ImnterfaceMultitoolGUI, ISubscriptionController, ICheckedInteractable<AiActivate>, IMultitoolMasterable, ICanOpenNetTab, IServerSpawn
	{
		[Header("Access Restrictions for ID")]
		[Tooltip("Is this door restricted?")]
		public bool restricted;

		private AccessRestrictions accessRestrictions;
		public AccessRestrictions AccessRestrictions => accessRestrictions;

		[SerializeField]
		private List<Turret> turrets = new List<Turret>();

		private SpriteHandler spriteHandler;
		private APCPoweredDevice apcPoweredDevice;

		private bool hasPower;
		public bool HasPower => hasPower;

		[SerializeField]
		private bool isOn = true;
		public bool IsOn => isOn;

		[SerializeField]
		private bool isStun = true;
		public bool IsStun => isStun;

		private void Awake()
		{
			//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			accessRestrictions = GetComponent<AccessRestrictions>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			apcPoweredDevice.OnStateChangeEvent += OnPowerStatusChange;
			ChangeTurretStates();
		}

		private void OnDisable()
		{
			apcPoweredDevice.OnStateChangeEvent -= OnPowerStatusChange;
		}

		public void AddTurretToSwitch(Turret turret)
		{
			turrets.Add(turret);
		}

		public void RemoveTurretFromSwitch(Turret turret)
		{
			turrets.Remove(turret);
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			if (interaction.ClickType != AiActivate.ClickTypes.CtrlClick && interaction.ClickType != AiActivate.ClickTypes.AltClick) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//If no power dont do anything
			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Turret switch has no power");
				return;
			}

			//Ctrl click switches it on or off
			if (interaction.ClickType == AiActivate.ClickTypes.CtrlClick)
			{
				ChangeOnState(!isOn);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {(isOn ? "On" : "Off")}");
				return;
			}

			//Else we must be alt clicking which is switch between stun and lethal
			ChangeStunState(!isStun);

			//If we are off send message saying so too
			if (isOn == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {(isStun ? "Stun" : "Lethal")}, but is turned off");
				return;
			}

			Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {(isStun ? "Stun" : "Lethal")}");
		}

		#endregion

		private void ChangePowerState(bool newState)
		{
			hasPower = newState;

			UpdateGui();

			if (newState)
			{
				ChangeOnState(isOn);
				return;
			}

			spriteHandler.ChangeSprite(0);
		}

		public void ChangeOnState(bool newState)
		{
			isOn = newState;

			UpdateGui();

			//Only change if we have power
			if (hasPower == false) return;

			if (newState)
			{
				ChangeStunState(isStun);
				return;
			}

			spriteHandler.ChangeSprite(1);

			ChangeTurretStates();
		}

		public void ChangeStunState(bool newState)
		{
			isStun = newState;

			UpdateGui();

			//Only change if we have power and on
			if (hasPower == false || isOn == false) return;

			// 2 = stun, 3 = lethal
			spriteHandler.ChangeSprite(newState ? 2 : 3);

			ChangeTurretStates();
		}

		private void ChangeTurretStates()
		{
			foreach (var turret in turrets)
			{
				if (turret == null)
				{
					Loggy.LogError($"null turrets in Turret switch at {this.transform.localPosition}");
					continue;
				}

				if (isOn == false)
				{
					turret.ChangeBulletState(Turret.TurretState.Off);
					continue;
				}

				turret.ChangeBulletState(isStun ? Turret.TurretState.Stun : Turret.TurretState.Lethal);
			}
		}

		private void OnPowerStatusChange(PowerState old , PowerState newStates)
		{
			ChangePowerState(newStates != PowerState.Off);
		}

		//Called when player wants to open nettab, so we can validate access
		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (accessRestrictions != null && restricted)
			{
				//Ai always allowed through, check other players access
				if (playerObject.GetComponent<PlayerScript>().PlayerType != PlayerTypes.Ai &&
				    accessRestrictions.CheckAccess(playerObject) == false)
				{
					Chat.AddExamineMsgFromServer(playerObject, "Higher Access Level Needed");
					return false;
				}
			}

			return true;
		}

		private void UpdateGui()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.TurretController);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();

			if (HasPower == false)
			{
				valuesToSend.Add(new ElementValue() { Id = "TextSetting", Value = Encoding.UTF8.GetBytes("No Power") });
			}
			else
			{
				valuesToSend.Add(new ElementValue() { Id = "TextSetting", Value = Encoding.UTF8.GetBytes(IsOn ? IsStun ? "Stun" : "Lethal" : "Off") });
			}

			valuesToSend.Add(new ElementValue() { Id = "SliderPower", Value = Encoding.UTF8.GetBytes((isOn ? 1 * 100 : 0).ToString()) });
			valuesToSend.Add(new ElementValue() { Id = "SliderStun", Value = Encoding.UTF8.GetBytes((isStun ? 0 : 1 * 100).ToString()) });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.TurretController, TabAction.Update, valuesToSend.ToArray());
		}

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.Turret;
		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		#region Editor

		void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;

			//Highlighting all controlled doors with red lines and spheres
			Gizmos.color = new Color(1, 0.5f, 0, 1);
			for (int i = 0; i < turrets.Count; i++)
			{
				var generalSwitchController = turrets[i];
				if (generalSwitchController == null) continue;
				Gizmos.DrawLine(sprite.transform.position, generalSwitchController.transform.position);
				Gizmos.DrawSphere(generalSwitchController.transform.position, 0.25f);
			}
		}

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var newTurret = potentialObject.GetComponent<Turret>();
				if (newTurret == null) continue;
				AddDoorControllerFromScene(newTurret);
				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		private void AddDoorControllerFromScene(Turret newTurret)
		{
			if (turrets.Contains(newTurret))
			{
				turrets.Remove(newTurret);
			}
			else
			{
				turrets.Add(newTurret);
			}
		}

		#endregion
	}
}
