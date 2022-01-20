using System.Collections;
using System.Collections.Generic;
using Systems.Explosions;
using Items.Devices;
using UnityEngine;
using Communications;
using Managers;

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

		private bool isAnchored;
		private bool isCharging;
		private float currentCharge;

		private void Awake()
		{
			objectBehaviour = GetComponent<ObjectBehaviour>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			pickupable = GetComponentInChildren<Pickupable>();
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
				if (isAnchored) UnAnchor();
				else Anchor();
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
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} makes a clicking sound as it anchors to the ground", gameObject);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, gameObject.AssumedWorldPosServer());
		}
		private void UnAnchor()
		{
			isAnchored = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.ServerSetPushable(true);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
			Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} makes a clicking sound as it unanchors from the ground", gameObject);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver, gameObject.AssumedWorldPosServer());
		}

		private void ToggleActivity()
		{
			if (isAnchored == false) return;
			if (isCharging)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				spriteHandler.SetSpriteSO(inactiveSpriteSO);
			}
			else
			{
				UpdateManager.Add(CheckForVoltage, voltageCheckTimeInSeconds);
				spriteHandler.SetSpriteSO(activeSpriteSO);
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
			if (isAnchored == false) return;
			var electricalData = gameObject.RegisterTile().Matrix.MetaDataLayer.Get(gameObject.RegisterTile().LocalPosition)?.ElectricalData;
			if (electricalData != null)
			{
				foreach (var data in electricalData)
				{
					if(data.InData.Data.ActualVoltage > 0)
					{
						currentCharge += data.InData.Data.ActualVoltage * chargeAmplifer;
					}
				}
			}
			else
			{
				Logger.LogError($"Unable to find electrical data for {gameObject.ExpensiveName()} at {gameObject.RegisterTile().WorldPosition}!");
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				return;
			}
			CheckForOverCharge();
		}

		private void CheckForOverCharge()
		{
			if (Application.isEditor)
			{
				Logger.Log($"Powersink current charge is {currentCharge} out of {overchargeCap}." +
					$" Explosion damage at this rate is {currentCharge * explosionAmplifer}");
			}
			if(currentCharge > overchargeCap / 2)
			{
				SparkUtil.TrySpark(gameObject, 25f);
			}
			if(currentCharge > overchargeCap)
			{
				//Ensure that we remove this update call before the thing exploads so it doesn't try
				//Doing it twice in a laggy situation or worse; spam NREs.
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