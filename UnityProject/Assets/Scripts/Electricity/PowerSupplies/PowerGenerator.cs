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
	private float PlasmaConsumptionRate = 0.02f;

	private RegisterTile registerTile;
	private ObjectBehaviour objectBehaviour;
	private ItemStorage itemStorage;
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
	private bool isOn = false;
	private SolidPlasma burningSheet;
	public bool IsFueled => itemSlot.IsOccupied || burningSheet != null;

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
		objectBehaviour = GetComponent<ObjectBehaviour>();
		itemStorage = GetComponent<ItemStorage>();
		securable = GetComponent<WrenchSecurable>();
		baseSpriteHandler = GetComponentInChildren<SpriteHandler>();
		electricalNodeControl = GetComponent<ElectricalNodeControl>();
	}

	public override void OnStartServer()
	{
		itemSlot = itemStorage.GetIndexedItemSlot(0);
		securable.OnAnchoredChange.AddListener(OnSecuredChanged);
		registerTile.WaitForMatrixInit(CheckStartingPlasma);
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

	/// <summary>
	/// Map solid plasma so that it is sitting on the same tile as the generator for it to be added
	/// to the starting plasma amounts.false Server Only.
	/// </summary>
	private void CheckStartingPlasma(MatrixInfo matrixInfo)
	{
		var plasmaObjs = matrixInfo.Matrix.Get<SolidPlasma>(registerTile.LocalPositionServer, true);
		foreach (var plasma in plasmaObjs)
		{
			Inventory.ServerAdd(plasma.gameObject, itemSlot);
		}

		if (startAsOn)
		{
			TryToggleOn();
		}
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

	private bool TryBurnFuel()
	{
		if (IsFueled)
		{
			burningSheet = itemSlot.ItemObject.GetComponent<SolidPlasma>();			
			burningSheet.StartBurningPlasma(PlasmaConsumptionRate, FuelExhaustedEvent);
			return true;
		}

		return false;
	}

	//Server Only
	private void FuelExhaustedEvent()
	{
		Inventory.ServerConsume(itemSlot, 1);
		burningSheet = null;

		if (isOn)
		{
			if (!TryBurnFuel())
			{
				ToggleOff();
			}
		}
	}

	#region Interaction

	public string Examine(Vector3 worldPos = default)
	{
		return $"The generator is {(IsFueled ? "fueled" : "unfueled")} and " +
				$"{(isOn ? "running" : "not running")}.";
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (interaction.HandObject != null &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
		{
			Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		}
		else if (securable.IsAnchored)
		{
			TryTogglePower();
		}
	}

	#endregion Interaction

	private void TryToggleOn()
	{
		if (TryBurnFuel())
		{
			ToggleOn();
		}
	}

	private void ToggleOn()
	{
		electricalNodeControl.TurnOnSupply();
		baseSpriteHandler.ChangeSprite((int)SpriteState.On);
		isOn = true;
	}

	private void ToggleOff()
	{
		baseSpriteHandler.ChangeSprite((int)SpriteState.Off);
		electricalNodeControl.TurnOffSupply();
		if (burningSheet != null)
		{
			burningSheet.StopBurningPlasma();
		}
		isOn = false;
	}

	private void TryTogglePower()
	{
		if (!isOn)
		{
			TryToggleOn();
		}
		else
		{
			ToggleOff();
		}
	}
}
