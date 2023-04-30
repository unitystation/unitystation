using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity.NodeModules;
using Systems.Interaction;


public enum BatteryStateSprite
{
	Full,
	Half,
	Empty,
}

namespace Objects.Engineering
{
	public class DepartmentBattery : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl, ICheckedInteractable<AiActivate>
	{
		public DepartmentBatterySprite CurrentSprite = DepartmentBatterySprite.Default;
		public SpriteRenderer Renderer;

		public Sprite BatteryOpenPresent;
		public Sprite BatteryOpenMissing;
		public Sprite BatteryClosedMissing;

		public Sprite BatteryCharged;
		public Sprite PartialCharge;
		[SyncVar(hook = nameof(UpdateBattery))]
		public BatteryStateSprite CurrentState;

		public Sprite LightOn;
		public Sprite LightOff;
		public Sprite LightRed;

		public SpriteRenderer BatteryCompartmentSprite;
		public SpriteRenderer BatteryIndicatorSprite;
		public SpriteRenderer PowerIndicator;

		public List<DepartmentBatterySprite> enums;
		public List<Sprite> Sprite;
		public Dictionary<DepartmentBatterySprite, Sprite> Sprites = new Dictionary<DepartmentBatterySprite, Sprite>();

		public ElectricalNodeControl ElectricalNodeControl;
		public BatterySupplyingModule BatterySupplyingModule;

		[SyncVar(hook = nameof(UpdateState))]
		public bool isOn = true;

		private bool hasInit;


		private void Start()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (hasInit) return;
			for (int i = 0; i < enums.Count; i++)
			{
				Sprites[enums[i]] = Sprite[i];
			}

			if (enums.Count > 0)
			{
				Renderer.sprite = Sprites[CurrentSprite];
			}

			hasInit = true;
			UpdateServerState();
		}

		public override void OnStartClient()
		{
			EnsureInit();
			base.OnStartClient();
			UpdateState(isOn, isOn);
		}

		public void PowerNetworkUpdate()
		{
			BatteryStateSprite newState;

			if (BatterySupplyingModule.CurrentCapacity <= 0)
			{
				newState = BatteryStateSprite.Empty;
			}
			else if (BatterySupplyingModule.CurrentCapacity <= (BatterySupplyingModule.CapacityMax / 2))
			{
				newState = BatteryStateSprite.Half;
			}
			else
			{
				newState = BatteryStateSprite.Full;
			}

			if (CurrentState != newState)
			{
				UpdateBattery(CurrentState, newState);
			}
		}

		private void UpdateBattery(BatteryStateSprite oldState, BatteryStateSprite State)
		{
			EnsureInit();
			CurrentState = State;

			if (BatteryIndicatorSprite == null) return;

			switch (CurrentState)
			{
				case BatteryStateSprite.Full:
					if (BatteryIndicatorSprite.enabled == false)
					{
						BatteryIndicatorSprite.enabled = true;
					}
					BatteryIndicatorSprite.sprite = BatteryCharged;
					break;
				case BatteryStateSprite.Half:
					if (BatteryIndicatorSprite.enabled == false)
					{
						BatteryIndicatorSprite.enabled = true;
					}
					BatteryIndicatorSprite.sprite = PartialCharge;
					break;
				case BatteryStateSprite.Empty:
					BatteryIndicatorSprite.enabled = false;
					break;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			isOn = !isOn;
			UpdateServerState();
		}

		public void UpdateServerState()
		{
			if (isOn)
			{
				ElectricalNodeControl.TurnOnSupply();
			}
			else
			{
				ElectricalNodeControl.TurnOffSupply();
			}
		}

		public void UpdateState(bool _wasOn, bool _isOn)
		{
			EnsureInit();
			isOn = _isOn;
			if (isOn)
			{
				PowerIndicator.sprite = LightOn;
			}
			else
			{
				PowerIndicator.sprite = LightOff;
			}
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			isOn = !isOn;
			UpdateServerState();
		}

		#endregion
	}
}
