using System;
using System.Collections.Generic;
using Communications;
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Physics;
using US13.Core.Sprite_Handler;
using US13.Items.Devices;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Systems.Electricity.Electrical_processes;
using US13.Systems.Electricity.NodeModules;
using US13.Systems.Inventory;
using Util;

namespace Traitor
{
	public class PowerBeacon : SignalReceiver
	{
		[SerializeField] private float voltageCheckTimeInSeconds = 0.2f;

		private SpriteHandler spriteHandler;
		private UniversalObjectPhysics objectBehaviour;
		private Pickupable pickupable;

		private ResistanceSourceModule RR;

		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private SpriteDataSO inactiveSpriteSO;

		[SyncVar] private bool isAnchored;
		[SyncVar] private bool isActive;


		public static List<PowerBeacon> ActivePowerBeacons = new List<PowerBeacon>();


		private void Awake()
		{
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			pickupable = GetComponentInChildren<Pickupable>();
			RR = GetComponent<ResistanceSourceModule>();
			ActivePowerBeacons.Add(this);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.UsedObject == null)
			{
				ToggleActivity();
				return;
			}

			if (interaction.UsedObject.Item().HasTrait(CommonTraits.Instance.Screwdriver))
			{
				// TODO check that the plating is exposed and no objects in the way, via MatrixManager.IsConstructable() or something

				var pos = objectBehaviour.registerTile.LocalPositionServer;
				var electricalConnections = objectBehaviour.registerTile.Matrix.GetElectricalConnections(pos);
				if (electricalConnections?.List.Count == 0)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You screw the power sink down, but there are no cables to tap into!");
				}

				if (isAnchored)
				{
					UnAnchor();
				}
				else
				{
					Anchor();
				}

				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, gameObject.AssumedWorldPosServer());
				return;
			}

			if (interaction.UsedObject.TryGetComponent<RemoteSignaller>(out var signaller))
			{
				Emitter = signaller;
				Frequency = signaller.Frequency;
				Chat.AddExamineMsg(interaction.Performer,
					$"You pair the {interaction.UsedObject.ExpensiveName()} to this device.");
			}
		}

		private void Anchor()
		{
			isAnchored = true;
			pickupable.ServerSetCanPickup(false);
			objectBehaviour.SetIsNotPushable(true);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			Chat.AddActionMsgToChat(gameObject,
				$"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>anchors</b> to the ground.");
		}

		public void OnDestroy()
		{
			ActivePowerBeacons.Remove(this);
		}


		private void ToggleActivity()
		{
			if (isAnchored == false) return;
			if (isAnchored && isActive == false)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				spriteHandler.SetSpriteSO(inactiveSpriteSO);
				RR.Resistance = 10000f;
				ActivePowerBeacons.Remove(this);
			}
			else
			{
				UpdateManager.Add(CheckForVoltage, voltageCheckTimeInSeconds);
				spriteHandler.SetSpriteSO(activeSpriteSO);
				RR.Resistance = 100f;
				ActivePowerBeacons.Add(this);
			}

			isActive = !isActive;
		}

		public void CheckForVoltage()
		{
			var electricalData = gameObject.RegisterTile().Matrix.MetaDataLayer
				.Get(gameObject.RegisterTile().LocalPosition)?.ElectricalData;
			if (isAnchored == false || RR == null || RR.ControllingNode == null || electricalData == null)
			{
				if (isActive) ToggleActivity();
				UnAnchor();
				return;
			}
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter,
			ISignalMessage message = null)
		{
			ToggleActivity();
		}

		private void UnAnchor()
		{
			isAnchored = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.SetIsNotPushable(false);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
			Chat.AddActionMsgToChat(gameObject,
				$"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>unanchors</b> from the ground.");
		}
	}
}