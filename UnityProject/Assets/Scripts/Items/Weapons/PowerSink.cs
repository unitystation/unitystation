using System.Collections;
using System.Collections.Generic;
using Systems.Explosions;
using Items.Devices;
using UnityEngine;
using Communications;
using Managers;
using Mirror;
using Systems.Electricity;
using Systems.Electricity.NodeModules;

namespace Items.Weapons
{
	public class PowerSink : SignalReceiver, ICheckedInteractable<HandApply>
	{
		[SerializeField] private float overchargeCap = 250000;
		[SerializeField] private float explosionAmplifer = 0.15f;
		[SerializeField] private float chargeAmplifer = 1f;
		[SerializeField] private float voltageCheckTimeInSeconds = 0.2f;
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private SpriteDataSO inactiveSpriteSO;

		private SpriteHandler spriteHandler;
		private ObjectBehaviour objectBehaviour;
		private Pickupable pickupable;

		private ResistanceSourceModule RR;

		[SyncVar] private bool isAnchored;
		[SyncVar] private bool isCharging;
		private float currentCharge;

		private void Awake()
		{
			objectBehaviour = GetComponent<ObjectBehaviour>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			pickupable = GetComponentInChildren<Pickupable>();
			RR = GetComponent<ResistanceSourceModule>();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
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
				if (isAnchored && isCharging == false) UnAnchor();
				else Anchor();
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, gameObject.AssumedWorldPosServer());
				return;
			}
			if (interaction.UsedObject.Item().HasTrait(CommonTraits.Instance.Multitool))
			{
				Chat.AddExamineMsg(interaction.Performer, $"This device's battery unit is holding {currentCharge.ToEngineering("V")}");
				return;
			}
			if (interaction.UsedObject.TryGetComponent<RemoteSignaller>(out var signaller))
			{
				Emitter = signaller;
				Frequency = signaller.Frequency;
				Chat.AddExamineMsg(interaction.Performer, $"You pair the {interaction.UsedObject.ExpensiveName()} to this device.");
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		private void Anchor()
		{
			isAnchored = true;
			pickupable.ServerSetCanPickup(false);
			objectBehaviour.ServerSetPushable(false);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>anchors</b> to the ground", gameObject);
		}
		private void UnAnchor()
		{
			isAnchored = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.ServerSetPushable(true);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>unanchors</b> from the ground", gameObject);
		}

		private void ToggleActivity()
		{
			if (isAnchored == false) return;
			if (isCharging)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				spriteHandler.SetSpriteSO(inactiveSpriteSO);
				RR.Resistance = 10000f;
			}
			else
			{
				UpdateManager.Add(CheckForVoltage, voltageCheckTimeInSeconds);
				spriteHandler.SetSpriteSO(activeSpriteSO);
				RR.Resistance = 0.0001f;
			}
			isCharging = !isCharging;
		}

		private void Detonate()
		{
			// Get data before despawning
			var worldPos = gameObject.AssumedWorldPosServer();
			// Despawn the explosive
			_ = Despawn.ServerSingle(gameObject);
			Explosion.StartExplosion(worldPos.RoundToInt(), currentCharge * explosionAmplifer);
		}

		public void CheckForVoltage()
		{
			var electricalData = gameObject.RegisterTile().Matrix.MetaDataLayer.Get(gameObject.RegisterTile().LocalPosition)?.ElectricalData;
			if (isAnchored == false || RR == null || RR.ControllingNode == null || electricalData == null)
			{
				if(isCharging) ToggleActivity();
				UnAnchor();
				return;
			}
			foreach (var data in electricalData)
			{
				if(data.InData.Data.ActualVoltage == 0) continue;
				currentCharge += data.InData.Data.ActualVoltage * chargeAmplifer;
			}
			CheckForOverCharge();
		}

		private void CheckForOverCharge()
		{
			if(currentCharge > overchargeCap / 2)
			{
				SparkUtil.TrySpark(gameObject, 25f);
			}
			if(currentCharge > overchargeCap)
			{
				//Ensure that we remove this update call before the thing exploads so it doesn't try Doing it twice
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				Detonate();
			}
		}

		public override void ReceiveSignal(SignalStrength strength, ISignalMessage message = null)
		{
			ToggleActivity();
		}
	}
}
