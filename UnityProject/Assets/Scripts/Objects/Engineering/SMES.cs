using System;
using System.Collections;
using Systems.Electricity.NodeModules;
using Systems.Explosions;
using Core.Input_System.InteractionV2.Interactions;
using Mirror;
using UnityEngine;
using ScriptableObjects;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ElectricalNodeControl))]
	[RequireComponent(typeof(BatterySupplyingModule))]
	public class SMES : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<AiActivate>, INodeControl, IExaminable
	{
		[Tooltip("How often (in seconds) the SMES's charging status should be updated.")]
		[SerializeField]
		[Range(1, 20)]
		private int indicatorUpdatePeriod = 5;
		private RegisterTile registerTile;
		private ObjectBehaviour objectBehaviour;

		private ElectricalNodeControl electricalNodeControl;
		private BatterySupplyingModule batterySupplyingModule;
		private GameObject currentSparkEffect;


		private SpriteHandler baseSpriteHandler;
		// Overlays
		private SpriteHandler chargingIndicator;
		private SpriteHandler outputEnabledIndicator;
		private SpriteHandler chargeLevelIndicator;

		private bool IsCharging => batterySupplyingModule.ChargingDivider > 0.1f;
		private float MaxCharge => batterySupplyingModule.CapacityMax;
		private float CurrentCharge => batterySupplyingModule.CurrentCapacity;
		private int ChargePercent => Convert.ToInt32(Math.Round(CurrentCharge * 100 / MaxCharge));


		private bool outputEnabled = false;

		private enum SpriteState
		{
			Normal = 0,
			CellsExposed = 1
		}

		private enum OutputEnabledOverlayState
		{
			OutputEnabled = 0,
			SMESNoCells = 1
		}

		private enum ChargingOverlayState
		{
			Discharging = 0,
			Charging = 1
		}

		#region Lifecycle

		private void Awake()
		{
			baseSpriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();
			chargingIndicator = transform.GetChild(1).GetComponent<SpriteHandler>();
			outputEnabledIndicator = transform.GetChild(2).GetComponent<SpriteHandler>();
			chargeLevelIndicator = transform.GetChild(3).GetComponent<SpriteHandler>();
			registerTile = GetComponent<RegisterTile>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			batterySupplyingModule = GetComponent<BatterySupplyingModule>();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			outputEnabled = batterySupplyingModule.StartOnStartUp;
			UpdateMe();
			UpdateManager.Add(UpdateMe, indicatorUpdatePeriod);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		#endregion Lifecycle

		private void UpdateMe()
		{
			UpdateChargingIndicator();
			UpdateChargeLevelIndicator();
		}

		private void UpdateChargingIndicator()
		{
			if (IsCharging)
			{
				chargingIndicator.ChangeSprite((int) ChargingOverlayState.Charging);
			}
			else
			{
				chargingIndicator.ChangeSprite((int) ChargingOverlayState.Discharging);
			}
		}

		private void UpdateChargeLevelIndicator()
		{
			int chargeIndex = Convert.ToInt32(Math.Round((ChargePercent / 100f) * 4));
			chargeLevelIndicator.ChangeSprite(chargeIndex);
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			UpdateMe();
			return $"The charge indicator shows a {ChargePercent} percent charge. " +
			       $"The input level is: {batterySupplyingModule.InputLevel} % The output level is: {batterySupplyingModule.OutputLevel} %. " +
			       $"The power input/output is " +
			       $"{(outputEnabled ? $"enabled, and it seems to {(IsCharging ? "be" : "not be")} charging" : "disabled")}. " +
			       "Use a crowbar to adjust the output level and a wrench to adjust the input level.";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)) return true;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;
			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
			{
				ServerToggleInputLevel(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				ServerToggleOutputLevel(interaction);
			}
			else
			{
				ServerToggleOutputMode();
			}
		}

		#endregion Interaction

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			ServerToggleOutputMode();
		}

		#endregion

		private void ServerToggleOutputMode()
		{
			TrySpark();
			if (outputEnabled)
			{
				ServerToggleOutputModeOff();
			}
			else
			{
				ServerToggleOutputModeOn();
			}
		}

		private void ServerToggleInputLevel(HandApply interaction)
		{
			//TrySpark();
			if (!outputEnabled)
			{
				var worldPos = registerTile.WorldPositionServer;
				SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Tick, worldPos, sourceObj: gameObject);
				if (batterySupplyingModule.InputLevel < 100)
				{
					batterySupplyingModule.InputLevel++;
				}
				else
				{
					batterySupplyingModule.InputLevel = 0;
				}
			}
			else
			{
				TrySpark();
			}
		}


		private void ServerToggleOutputLevel(HandApply interaction)
		{
			if (!outputEnabled)
			{
				var worldPos = registerTile.WorldPositionServer;
				SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Tick, worldPos, sourceObj: gameObject);
				if (batterySupplyingModule.OutputLevel < 100)
				{
					batterySupplyingModule.OutputLevel++;
				}
				else
				{
					batterySupplyingModule.OutputLevel = 0;
				}
			}
			else
			{
				TrySpark();
			}
		}

		private void ServerToggleOutputModeOn()
		{
			outputEnabledIndicator.ChangeSprite((int) OutputEnabledOverlayState.OutputEnabled);
			outputEnabledIndicator.PushTexture();
			electricalNodeControl.TurnOnSupply();
			outputEnabled = true;
		}

		private void ServerToggleOutputModeOff()
		{
			outputEnabledIndicator.PushClear();
			electricalNodeControl.TurnOffSupply();
			outputEnabled = false;
		}

		public void PowerNetworkUpdate() { }

		private void TrySpark()
		{
			//Not already doing an effect
			if (currentSparkEffect != null) return;

			currentSparkEffect = SparkUtil.TrySpark(objectBehaviour);
		}
	}
}
