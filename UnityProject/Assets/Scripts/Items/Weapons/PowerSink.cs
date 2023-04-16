using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
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
	public class PowerSink : SignalReceiver, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private float overchargeCap = 250000;
		[SerializeField] private float explosionAmplifer = 0.15f;
		[SerializeField] private float chargeAmplifer = 1f;
		[SerializeField] private float voltageCheckTimeInSeconds = 0.2f;
		[SerializeField] private SpriteDataSO activeSpriteSO;
		[SerializeField] private SpriteDataSO inactiveSpriteSO;
		[SerializeField] private AddressableAudioSource beepSound;

		private SpriteHandler spriteHandler;
		private UniversalObjectPhysics objectBehaviour;
		private Pickupable pickupable;

		private ResistanceSourceModule RR;

		[SyncVar] private bool isAnchored;
		[SyncVar] private bool isCharging;
		[SyncVar] private float currentCharge;

		private void Awake()
		{
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			pickupable = GetComponentInChildren<Pickupable>();
			RR = GetComponent<ResistanceSourceModule>();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
			StopCoroutine(BeepBeep());
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
					Chat.AddExamineMsgFromServer(interaction.Performer, "You screw the power sink down, but there are no cables to tap into!");
				}

				if (isAnchored && isCharging == false)
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
			objectBehaviour.SetIsNotPushable(true);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>anchors</b> to the ground.");
		}
		private void UnAnchor()
		{
			isAnchored = false;
			pickupable.ServerSetCanPickup(true);
			objectBehaviour.SetIsNotPushable(false);
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} makes a clicking sound as it <b>unanchors</b> from the ground.");
		}

		private void ToggleActivity()
		{
			if (isAnchored == false) return;
			if (isCharging)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckForVoltage);
				spriteHandler.SetSpriteSO(inactiveSpriteSO);
				RR.Resistance = 10000f;
				StopCoroutine(BeepBeep());
			}
			else
			{
				StartCoroutine(BeepBeep());
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

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			ToggleActivity();
		}

		private IEnumerator BeepBeep()
		{
			while (isCharging && gameObject != null)
			{
				SoundManager.PlayNetworkedAtPos(beepSound, gameObject.AssumedWorldPosServer());
				yield return WaitFor.Seconds(2f);
			}
		}

		public string Examine(Vector3 worldPos = default)
		{
			return isAnchored
				? "It is held in place with some <b>screws</b>."
				: "Some mounting <b>screws</b> are exposed on the underside.";
		}
	}
}
