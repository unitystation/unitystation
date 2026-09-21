using System;
using System.Collections;
using System.Text;
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Systems.Construction;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Electricity.NodeModules;
using US13.Systems.Explosions;
using US13.Tilemaps.Behaviours.Objects;
using Util;
using Util.Independent.FluentRichText;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;


namespace US13.Objects.Engineering
{
	[RequireComponent(typeof(ElectricalNodeControl))]
	[RequireComponent(typeof(BatterySupplyingModule))]
	public class SMES : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<AiActivate>, INodeControl, IExaminable, IEmpAble
	{
		[Tooltip("How often (in seconds) the SMES's charging status should be updated.")]
		[SerializeField]
		[Range(1, 20)]
		private int indicatorUpdatePeriod = 5;
		private RegisterTile registerTile;
		private UniversalObjectPhysics objectBehaviour;

		private ElectricalNodeControl electricalNodeControl;
		private BatterySupplyingModule batterySupplyingModule;
		private Machine machine;


		private SpriteHandler baseSpriteHandler;
		// Overlays
		private SpriteHandler chargingIndicator;
		private SpriteHandler outputEnabledIndicator;
		private SpriteHandler chargeLevelIndicator;

		private bool IsCharging => batterySupplyingModule.ChargingWatts > 10f;
		private float MaxCharge => batterySupplyingModule.CapacityMax;
		private float CurrentCharge => batterySupplyingModule.GetSetCurrentCapacity;
		private int ChargePercent => Mathf.RoundToInt(CurrentCharge * 100 / MaxCharge);

		private bool isExploding = false;
		[SyncVar] private bool outputEnabled = false;

		public event Action<PowerState, PowerState> OnStateChangeEvent;
		private PowerState currentState = PowerState.Off;

		[SerializeField] private float overChargePercentage = 105.0f;
		[SerializeField] private float lowVoltagePercentage = 5.0f;

		[SerializeField] private Texture2D crowbarIcon = null;
		[SerializeField] private Texture2D wrenchIcon = null;

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
			chargingIndicator = transform.GetChild(2).GetComponent<SpriteHandler>();
			outputEnabledIndicator = transform.GetChild(3).GetComponent<SpriteHandler>();
			chargeLevelIndicator = transform.GetChild(4).GetComponent<SpriteHandler>();
			registerTile = GetComponent<RegisterTile>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			machine = GetComponent<Machine>();

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
				chargingIndicator?.SetCatalogueIndexSprite((int) ChargingOverlayState.Charging);
			}
			else
			{
				chargingIndicator?.SetCatalogueIndexSprite((int) ChargingOverlayState.Discharging);
			}
		}

		private void UpdateChargeLevelIndicator()
		{
			int chargeIndex = Convert.ToInt32(Math.Round((ChargePercent / 100f) * 4));
			chargeLevelIndicator.SetCatalogueIndexSprite(chargeIndex);
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			UpdateMe();
			StringBuilder examineText = new StringBuilder();
			examineText.AppendLine($"The charge indicator shows a {ChargePercent} percent charge. ");
			examineText.AppendLine($"The input level is: {batterySupplyingModule.InputLevel} %.");
			examineText.AppendLine($"The output level is: {batterySupplyingModule.OutputLevel} %.");
			examineText.AppendLine($"\nThe power input/output is " +
			                       $"{(outputEnabled ? $"enabled, and it seems to {(IsCharging ? "be" : "not be")} charging" : "disabled")}.");
			if (crowbarIcon != null && wrenchIcon != null)
			{
				examineText.AppendLine($"Use a <sprite name=\"{crowbarIcon.name}\"> crowbar to adjust the output level and a <sprite name=\"{wrenchIcon.name}\"> wrench to adjust the input level.".Color(RichTextColor.Yellow));
			}
			else
			{
				examineText.AppendLine($"Use a crowbar to adjust the output level and a wrench to adjust the input level.".Color(RichTextColor.Yellow));
			}
			examineText.AppendLine("Use alt-click while adjusting the levels to do increments of 15 instead of 1.".Color(RichTextColor.Yellow));
			return examineText.ToString();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
			{
				return !machine.GetPanelOpen();
			}
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
			var worldPos = registerTile.WorldPositionServer;
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Tick, worldPos, sourceObj: gameObject);
			if (batterySupplyingModule.InputLevel < 100)
			{
				batterySupplyingModule.InputLevel += interaction.IsAltClick ? 15 : 1;
			}
			else
			{
				batterySupplyingModule.InputLevel = 0;
			}
			Chat.AddExamineMsg(interaction.Performer, $"Changed the input level to {batterySupplyingModule.InputLevel} %.");
		}

		private void ServerToggleOutputLevel(HandApply interaction)
		{
			TrySpark();
			var worldPos = registerTile.WorldPositionServer;
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Tick, worldPos, sourceObj: gameObject);
			if (batterySupplyingModule.OutputLevel < 100)
			{
				batterySupplyingModule.OutputLevel += interaction.IsAltClick ? 15 : 1;
			}
			else
			{
				batterySupplyingModule.OutputLevel = 0;
			}
			Chat.AddExamineMsg(interaction.Performer, $"Changed the output level to {batterySupplyingModule.OutputLevel} %.");
		}

		private void ServerToggleOutputModeOn()
		{
			outputEnabledIndicator.SetCatalogueIndexSprite((int) OutputEnabledOverlayState.OutputEnabled);
			outputEnabledIndicator.PushTexture();
			electricalNodeControl.TurnOnSupply();
			outputEnabled = true;
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} humms as it starts outputting power.");
		}

		private void ServerToggleOutputModeOff()
		{
			outputEnabledIndicator.PushClear();
			electricalNodeControl.TurnOffSupply();
			outputEnabled = false;
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} steadily goes quite as it stops attempting to output power.");
		}

		public void PowerNetworkUpdate()
		{
			SetPowerStateFromVoltage();
		}

		public PowerState SetPowerStateFromVoltage()
		{
			PowerState newState = PowerStateFromChargingPercent();

			if (newState == currentState) return currentState;
			OnStateChangeEvent?.Invoke(currentState, newState);
			currentState = newState;
			return currentState;
		}

		private PowerState PowerStateFromChargingPercent()
		{
			PowerState newState;
			if (ChargePercent <= 0.5f)
			{
				newState = PowerState.Off;
			}
			else if (ChargePercent <= lowVoltagePercentage)
			{
				newState = PowerState.LowVoltage;
			}
			else if (ChargePercent >= overChargePercentage)
			{
				newState = PowerState.OverVoltage;
			}
			else
			{
				newState = PowerState.On;
			}

			return newState;
		}

		private void TrySpark()
		{
			SparkUtil.TrySpark(gameObject);
		}

		public void OnEmp(int EmpStrength)
		{
			if (CurrentCharge > 10000000 && DMMath.Prob(25) && isExploding == false)
			{
				isExploding = true;
				TrySpark();
				Chat.AddActionMsgToChat(gameObject, $"<color=red>{gameObject.ExpensiveName()} starts to spit out sparks and smoke! No way this can end good...");
				StartCoroutine(Emp());
			}
			machine.BatteryChangeChargedByDelta(EmpStrength * 100000);
		}

		private IEnumerator Emp()
		{
			yield return WaitFor.Seconds(3);
			Explosion.StartExplosion(gameObject.GetComponent<RegisterObject>().WorldPosition,UnityEngine.Random.Range(100,300));
		}
	}
}
