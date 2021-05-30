using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Electricity;
using Core.Input_System.InteractionV2.Interactions;
using Electricity.Inheritance;
using Objects.Other;
using UnityEngine;

namespace Objects.Wallmounts.Switches
{
	[RequireComponent(typeof(AccessRestrictions))]
	[RequireComponent(typeof(APCPoweredDevice))]
	public class TurretSwitch : SubscriptionController, ICheckedInteractable<HandApply>, ICheckedInteractable<AiActivate>, ISetMultitoolMaster
	{
		[Header("Access Restrictions for ID")]
		[Tooltip("Is this door restricted?")]
		public bool restricted;

		private AccessRestrictions accessRestrictions;

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.Turret;
		public MultitoolConnectionType ConType => conType;

		private bool multiMaster = true;
		public bool MultiMaster => multiMaster;

		public void AddSlave(object SlaveObject)
		{
		}

		[SerializeField]
		private List<Turret> turrets = new List<Turret>();

		private SpriteHandler spriteHandler;
		private bool buttonCoolDown = false;
		private APCPoweredDevice apcPoweredDevice;

		[SerializeField]
		private TurretSwitchState turretSwitchState = TurretSwitchState.Stun;

		private TurretSwitchState cachedState = TurretSwitchState.Stun;

		private bool hasPower;

		private void Awake()
		{
			//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			accessRestrictions = GetComponent<AccessRestrictions>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			ChangeTurretStates();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			apcPoweredDevice.OnStateChangeEvent.AddListener(OnPowerStatusChange);
		}

		private void OnDisable()
		{
			apcPoweredDevice.OnStateChangeEvent.RemoveListener(OnPowerStatusChange);
		}

		public void AddTurretToSwitch(Turret turret)
		{
			turrets.Add(turret);
		}

		public void RemoveTurretFromSwitch(Turret turret)
		{
			turrets.Remove(turret);
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (side == NetworkSide.Client)
			{
				if (buttonCoolDown) return false;
				buttonCoolDown = true;
				StartCoroutine(CoolDown());
			}

			return true;
		}

		IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(0.5f);
			buttonCoolDown = false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (accessRestrictions != null && restricted)
			{
				if (accessRestrictions.CheckAccess(interaction.Performer) == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Higher Access Level Needed");
					return;
				}
			}

			if (hasPower == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Turret switch has no power");
				return;
			}

			ChangeState(turretSwitchState.Next());

			Chat.AddActionMsgToChat(interaction.Performer, $"You set the turret switch to {turretSwitchState}",
				$"{interaction.Performer.ExpensiveName()} sets the turret switch to {turretSwitchState}");
		}

		private void ChangePowerState(bool newState)
		{
			hasPower = newState;
			spriteHandler.ChangeSprite(hasPower ? (int)turretSwitchState + 1 : 0);
		}

		private void ChangeState(TurretSwitchState newState)
		{
			if (hasPower == false) return;

			turretSwitchState = newState;

			spriteHandler.ChangeSprite((int)turretSwitchState + 1);

			ChangeTurretStates();
		}

		private void ChangeTurretStates()
		{
			foreach (var turret in turrets)
			{
				//We can cast to the other enum, as they are the same length and have same
				//order of states
				turret.ChangeBulletState((Turret.TurretState)turretSwitchState);
			}
		}

		#endregion

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
				ChangeState(turretSwitchState == TurretSwitchState.Off ? cachedState : TurretSwitchState.Off);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {turretSwitchState}");
				return;
			}

			//Else we must be alt clicking which is switch between stun and lethal
			//If we are off switch cached state
			if (turretSwitchState == TurretSwitchState.Off)
			{
				cachedState = cachedState == TurretSwitchState.Stun ? TurretSwitchState.Lethal : TurretSwitchState.Stun;
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {cachedState}, but is turned off");
				return;
			}

			ChangeState(turretSwitchState == TurretSwitchState.Stun ? TurretSwitchState.Lethal : TurretSwitchState.Stun);

			Chat.AddExamineMsgFromServer(interaction.Performer, $"You set the turret switch to {turretSwitchState}");
		}

		#endregion

		private void OnPowerStatusChange(Tuple<PowerState, PowerState> newStates)
		{
			if (newStates.Item2 == PowerState.Off)
			{

				hasPower = false;
				ChangeState(TurretSwitchState.Off);
				return;
			}

			hasPower = true;
			ChangeState(cachedState);
		}

		private enum TurretSwitchState
		{
			Off,
			Stun,
			Lethal
		}

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

		public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
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
