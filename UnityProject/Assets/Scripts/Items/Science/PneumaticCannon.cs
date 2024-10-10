using System;
using AddressableReferences;
using Core.Physics;
using Objects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

public class PneumaticCannon : MonoBehaviour, ICheckedInteractable<InventoryApply>, ICheckedInteractable<AimApply>, ICheckedInteractable<HandActivate>, IServerSpawn
{
	private enum PressureSetting
	{
		Low = 25,
		Medium = 50,
		High = 100,
	}

	private ItemSlot canisterSlot;
	private GasContainer canister = null;
	private ItemSlot itemSlot;

	private const float CANNON_MAX_PRESSURE = 20f; //In Hundreds of kiloPascals
	private const float CANNON_MIN_FUNCTIONAL_PRESSURE = 300; //In kiloPascals

	private PressureSetting pressureSetting = PressureSetting.Low;

	[SerializeField] private ItemTrait canisterTrait;
	[SerializeField] private Size maximumLoadableSize = Size.Large;

	[SerializeField] private SpriteHandler canisterSpriteHandler = null;
	[SerializeField] private SpriteHandler bindingSprite = null;

	[SerializeField] private AddressableAudioSource firingSound = null;

	private bool isOnCooldown => Time.time - lastShootTime < SHOOT_COOLDOWN_TIME;

	private float lastShootTime = 0;
	private const float SHOOT_COOLDOWN_TIME = 1f;


	private void Awake()
	{
		var itemStorage = GetComponent<ItemStorage>();
		canisterSlot = itemStorage.GetIndexedItemSlot(0);
		itemSlot = itemStorage.GetIndexedItemSlot(1);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		UpdateCanisterSprites(null);
	}

	private void UpdateCanisterSprites(GasContainer gasContainer)
	{
		if(gasContainer == null)
		{
			canisterSpriteHandler.PushClear();
			bindingSprite.PushClear();
			return;
		}

		if (gasContainer.gameObject.GetAllChildren()[0].TryGetComponent<SpriteHandler>(out var handler) == false) return;

		var spriteSO = handler.GetCurrentSpriteSO();
		canisterSpriteHandler.SetSpriteSO(spriteSO, networked: true);

		canisterSpriteHandler.PushTexture();
		bindingSprite.PushTexture();
	}

	#region Inventory Interactions

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot) return true;

		return false;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.TargetObject != gameObject || interaction.IsFromHandSlot == false) return;

		if (interaction.UsedObject != null) FullHandInteraction(interaction);
		else EmptyHandInteraction(interaction);
	}

	public void FullHandInteraction(InventoryApply interaction)
	{
		if (interaction.UsedObject.TryGetComponent<GasContainer>(out var canister))
		{
			if (this.canister == null)
			{
				this.canister = canister;
				Inventory.ServerTransfer(interaction.FromSlot, canisterSlot, ReplacementStrategy.Cancel);
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You load the {interaction.UsedObject.ExpensiveName()} into the {gameObject.ExpensiveName()}'s canister slot.");
				UpdateCanisterSprites(canister);
				return;
			}
			else
			{
				Chat.AddWarningMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} already contains a valid gas canister! Please remove the existing tank before attempting to attach another.");
				return;
			}
		}
		else if (itemSlot.IsEmpty == false)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} already contains an item in it's barrel! Please remove the existing item before attempting to load another.");
			return;
		}
		else
		{
			Inventory.ServerTransfer(interaction.FromSlot, itemSlot, ReplacementStrategy.Cancel);
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You load the {interaction.UsedObject.ExpensiveName()} into the {gameObject.ExpensiveName()}'s item slot.");
			return;
		}
	}

	public void EmptyHandInteraction(InventoryApply interaction)
	{
		if (itemSlot.IsEmpty == false)
		{
			Inventory.ServerTransfer(itemSlot, interaction.FromSlot, ReplacementStrategy.Cancel);
			return;
		}
		if (canisterSlot.IsEmpty == false)
		{
			Inventory.ServerTransfer(canisterSlot, interaction.FromSlot, ReplacementStrategy.Cancel);
			canister = null;
			UpdateCanisterSprites(canister);
			return;
		}
	}

	#endregion

	#region HandActivate Interactions

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		switch(pressureSetting)
		{
			case PressureSetting.Low:
				pressureSetting = PressureSetting.Medium;
				break;
			case PressureSetting.Medium:
				pressureSetting = PressureSetting.High;
				break;
			case PressureSetting.High:
				pressureSetting = PressureSetting.Low;
				break;
		}

		Chat.AddExamineMsgFromServer(interaction.Performer, $"You adjust the {gameObject.ExpensiveName()}'s pressure regulator to {(int)pressureSetting * CANNON_MAX_PRESSURE}kPa");
	}

	#endregion

	#region AimApply Interactions

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (isOnCooldown == false && interaction.MouseButtonState == MouseButtonState.PRESS && interaction.Intent == Intent.Harm) return true;

		return false;
	}

	public void ServerPerformInteraction(AimApply interaction)
	{
		lastShootTime = Time.time;
		if(canister == null)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, "You attempt to fire the cannon but no canister is loaded!");
			return;
		}
		if (canister.ServerInternalPressure < CANNON_MIN_FUNCTIONAL_PRESSURE)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, "You attempt to fire the cannon but the gas pressure was too low!");
			return;
		}

		float percentDischarge = Mathf.Clamp(((int)pressureSetting * CANNON_MAX_PRESSURE) / canister.GasMixLocal.Pressure, 0, 1);
		float pressureToDischarge = Math.Min((int)pressureSetting * CANNON_MAX_PRESSURE, canister.GasMixLocal.Pressure);

		float molesToTransfer = percentDischarge * canister.GasMixLocal.Moles;
		var metaDataLayer = MatrixManager.AtPoint(interaction.Performer.RegisterTile().WorldPositionServer, true).MetaDataLayer;
		var metaNode = metaDataLayer.Get(interaction.Performer.RegisterTile().LocalPositionServer);

		GasMix mixToEffect = metaNode.GasMixLocal;
		GasMix.TransferGas(mixToEffect, canister.GasMixLocal, molesToTransfer);

		SoundManager.PlayNetworkedAtPos(firingSound, interaction.originatorPosition);
		if (pressureToDischarge >= (int)PressureSetting.High * CANNON_MAX_PRESSURE) interaction.PerformerPlayerScript.RegisterPlayer.ServerStun(dropItem: false);

		if (itemSlot.IsEmpty) return;

		//We assume a barrel area of 0.1 square meters applied for 1/10th of a second.
		var physics = itemSlot.ItemObject.GetComponent<UniversalObjectPhysics>();
		if (physics == null) return;

		Inventory.ServerDrop(itemSlot);
		physics.NewtonianNewtonPush(interaction.TargetVector, pressureToDischarge * 0.05f, 0.5f, 0.5f, interaction.TargetBodyPart, gameObject, 0.1f);
	}

	#endregion
}
