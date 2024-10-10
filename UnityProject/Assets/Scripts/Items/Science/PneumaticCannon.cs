using System;
using AddressableReferences;
using Core.Physics;
using Objects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

public class PneumaticCannon : MonoBehaviour, ICheckedInteractable<InventoryApply>, ICheckedInteractable<MouseDrop>, ICheckedInteractable<AimApply>, ICheckedInteractable<HandActivate>, IServerSpawn
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

	[SerializeField] private AddressableAudioSource dryFiringSound = null;
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
		Debug.Log("Will Interact");
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot) return true;

		return false;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.TargetObject != gameObject || interaction.IsFromHandSlot == false) return;

		if (interaction.UsedObject != null) FullHandInteraction(interaction.FromSlot, interaction.Performer, interaction.UsedObject);
		else EmptyHandInteraction(interaction.FromSlot);
	}

	public void FullHandInteraction(ItemSlot fromSlot, GameObject performer, GameObject usedObject)
	{
		if (usedObject.TryGetComponent<GasContainer>(out var canister))
		{
			if (this.canister == null)
			{
				this.canister = canister;
				Inventory.ServerTransfer(fromSlot, canisterSlot, ReplacementStrategy.DropOther);
				Chat.AddExamineMsgFromServer(performer, $"You load the {usedObject.ExpensiveName()} into the {gameObject.ExpensiveName()}'s canister slot.");
				UpdateCanisterSprites(canister);
				return;
			}
			else
			{
				Chat.AddWarningMsgFromServer(performer, $"The {gameObject.ExpensiveName()} already contains a valid gas canister! Please remove the existing tank before attempting to attach another.");
				return;
			}
		}
		else if (itemSlot.IsEmpty == false)
		{
			Chat.AddWarningMsgFromServer(performer, $"The {gameObject.ExpensiveName()} already contains an item in it's barrel! Please remove the existing item before attempting to load another.");
			return;
		}
		else
		{
			Inventory.ServerTransfer(fromSlot, itemSlot, ReplacementStrategy.DropOther);
			Chat.AddExamineMsgFromServer(performer, $"You load the {usedObject.ExpensiveName()} into the {gameObject.ExpensiveName()}'s item slot.");
			return;
		}
	}

	public void EmptyHandInteraction(ItemSlot fromSlot)
	{
		if (itemSlot.IsEmpty == false)
		{
			Inventory.ServerTransfer(itemSlot, fromSlot, ReplacementStrategy.Cancel);
			return;
		}
		if (canisterSlot.IsEmpty == false)
		{
			Inventory.ServerTransfer(canisterSlot, fromSlot, ReplacementStrategy.Cancel);
			canister = null;
			UpdateCanisterSprites(canister);
			return;
		}
	}

	#endregion

	#region MouseDrop Interactions

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject && interaction.IsFromInventory) return true;

		return false;
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		if (interaction.TargetObject != gameObject || interaction.IsFromInventory == false) return;

		if (interaction.UsedObject != null) FullHandInteraction(interaction.FromSlot, interaction.Performer, interaction.UsedObject);
		else EmptyHandInteraction(interaction.FromSlot);
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
		bool fireFailed = false;
		if(canister == null)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, $"You attempt to fire the cannon {gameObject.ExpensiveName()} no canister is loaded!");
			fireFailed = true;
		}
		else if (canister.ServerInternalPressure < CANNON_MIN_FUNCTIONAL_PRESSURE)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, $"You attempt to fire the cannon {gameObject.ExpensiveName()} the gas pressure was too low!");
			fireFailed = true;
		}
		else if (itemSlot.IsEmpty)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, $"You attempt to fire the {gameObject.ExpensiveName()} but no item was loaded!");
			fireFailed = true;
		}

		if(fireFailed)
		{
			SoundManager.PlayNetworkedAtPos(dryFiringSound, transform.position, sourceObj: interaction.Performer);
			return;
		}

		Chat.AddActionMsgToChat(interaction.Performer,
					$"You fire your {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} fires their {gameObject.ExpensiveName()}");

		lastShootTime = Time.time;
		float percentDischarge = Mathf.Clamp(((int)pressureSetting * CANNON_MAX_PRESSURE) / canister.GasMixLocal.Pressure, 0, 1);
		float pressureToDischarge = Math.Min((int)pressureSetting * CANNON_MAX_PRESSURE, canister.GasMixLocal.Pressure);

		float molesToTransfer = percentDischarge * canister.GasMixLocal.Moles;
		var metaDataLayer = MatrixManager.AtPoint(interaction.Performer.RegisterTile().WorldPositionServer, true).MetaDataLayer;
		var metaNode = metaDataLayer.Get(interaction.Performer.RegisterTile().LocalPositionServer);

		GasMix mixToEffect = metaNode.GasMixLocal;
		GasMix.TransferGas(mixToEffect, canister.GasMixLocal, molesToTransfer);

		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Valve, transform.position, sourceObj: interaction.Performer);
		if (pressureToDischarge >= (int)PressureSetting.High * CANNON_MAX_PRESSURE) interaction.PerformerPlayerScript.RegisterPlayer.ServerStun(dropItem: false);

		//We assume a barrel area of 0.1 square meters applied for 1/10th of a second.
		var physics = itemSlot.ItemObject.GetComponent<UniversalObjectPhysics>();
		if (physics == null) return;

		Inventory.ServerDrop(itemSlot);
		physics.NewtonianNewtonPush(interaction.TargetVector, pressureToDischarge * 0.05f, 0.5f, 0.5f, interaction.TargetBodyPart, gameObject, 0.1f);
	}

	#endregion
}
