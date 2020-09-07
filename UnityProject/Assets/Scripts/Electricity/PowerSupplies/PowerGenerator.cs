using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PowerGenerator : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl, IExaminable
{
	[Tooltip("Whether this generator should start running when spawned.")]
	[SerializeField]
	private bool startAsOn = false;

	[Tooltip("The rate of fuel this generator should consume.")]
	[Range(0.01f, 0.1f)]
	[SerializeField]
	private float plasmaConsumptionRate = 0.02f;

	private RegisterTile registerTile;
	private ItemSlot itemSlot;
	private WrenchSecurable securable;
	private SpriteHandler baseSpriteHandler;
	private ElectricalNodeControl electricalNodeControl;

	[SerializeField]
	private AudioSource generatorRunSfx = default;
	[SerializeField]
	private AudioSource generatorEndSfx = default;
	[SerializeField]
	private ParticleSystem smokeParticles = default;

	[SyncVar(hook = nameof(OnSyncState))]
	private bool isOn;
	private float fuelAmount;
	private float fuelPerSheet = 10f;

	private enum SpriteState
	{
		Unsecured = 0,
		Off = 1,
		On = 2
	}

	#region Lifecycle

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		securable = GetComponent<WrenchSecurable>();
		baseSpriteHandler = GetComponentInChildren<SpriteHandler>();
		electricalNodeControl = GetComponent<ElectricalNodeControl>();
	}

	public override void OnStartServer()
	{
		var itemStorage = GetComponent<ItemStorage>();
		itemSlot = itemStorage.GetIndexedItemSlot(0);
		securable.OnAnchoredChange.AddListener(OnSecuredChanged);
		if (startAsOn)
		{
			fuelAmount = fuelPerSheet;
			TryToggleOn();
		}
	}
	#endregion Lifecycle

	public void PowerNetworkUpdate() { }

	private void OnSecuredChanged()
	{
		if (securable.IsAnchored)
		{
			baseSpriteHandler.ChangeSprite((int) SpriteState.Off);
		}
		else
		{
			ToggleOff();
			baseSpriteHandler.ChangeSprite((int) SpriteState.Unsecured);
		}

		ElectricalManager.Instance.electricalSync.StructureChange = true;
	}

	private void OnSyncState(bool oldState, bool newState)
	{
		isOn = newState;
		if (isOn)
		{
			baseSpriteHandler.PushTexture();
			generatorRunSfx.Play();
			smokeParticles.Play();
		}
		else
		{
			generatorRunSfx.Stop();
			smokeParticles.Stop();
			generatorEndSfx.Play();
		}
	}

	#region Interaction

	public string Examine(Vector3 worldPos = default)
	{
		return $"The generator is {(HasFuel() ? "fueled" : "unfueled")} and " +
				$"{(isOn ? "running" : "not running")}.";
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null && !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
		{
			int amountTransfered;
			var handStackable = interaction.HandObject.GetComponent<Stackable>();
			if (itemSlot.Item)
			{
				var stackable = itemSlot.Item.GetComponent<Stackable>();
				if (stackable.SpareCapacity == 0)
				{
					Chat.AddWarningMsgFromServer(interaction.Performer, "The generator sheet storage is full!");
					return;
				}
				amountTransfered = stackable.ServerCombine(handStackable);
			}
			else
			{
				amountTransfered = handStackable.Amount;
				Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
			}
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You fill the generator sheet storage with {amountTransfered.ToString()} more.");
		}
		else if (securable.IsAnchored)
		{
			if (!isOn)
			{
				if (!TryToggleOn())
				{
					Chat.AddWarningMsgFromServer(interaction.Performer, $"The generator needs more fuel!");
				}
			}
			else
			{
				ToggleOff();
			}
		}
	}

	#endregion Interaction

	void UpdateMe()
	{
		fuelAmount -= Time.deltaTime * plasmaConsumptionRate;
		if (fuelAmount <= 0)
		{
			ConsumeSheet();
		}
	}

	private void ConsumeSheet()
	{
		if (Inventory.ServerConsume(itemSlot, 1))
		{
			fuelAmount += fuelPerSheet;
		}
		else
		{
			ToggleOff();
		}
	}
	private bool TryToggleOn()
	{
		if (fuelAmount > 0 || itemSlot.Item)
		{
			ToggleOn();
			return true;
		}
		return false;
	}

	private bool HasFuel()
	{
		if (fuelAmount > 0 || itemSlot.Item)
		{
			return true;
		}
		return false;
	}

	private void ToggleOn()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		electricalNodeControl.TurnOnSupply();
		baseSpriteHandler.ChangeSprite((int)SpriteState.On);
		isOn = true;
	}

	private void ToggleOff()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		electricalNodeControl.TurnOffSupply();
		baseSpriteHandler.ChangeSprite((int)SpriteState.Off);
		isOn = false;
	}

	void OnDisable()
	{
		if (isOn)
		{
			ToggleOff();
		}
	}

}
