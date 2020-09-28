using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Autolathe : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IServerDespawn, IAPCPowered
{

	public PowerStates PoweredState;

	[SyncVar(hook = nameof(SyncSprite))]
	private AutolatheState stateSync;

	[SerializeField]
	private SpriteHandler spriteHandler = null;

	[SerializeField]
	private SpriteDataSO idleSprite = null;

	[SerializeField]
	private SpriteDataSO productionSprite = null;

	[SerializeField]
	private SpriteDataSO acceptingMaterialsSprite = null;

	private RegisterObject registerObject = null;

	[SerializeField]
	private MaterialStorage materialStorage = null;

	public MaterialStorage MaterialStorage { get => materialStorage; }

	[SerializeField]
	private MachineProductsCollection autolatheProducts = null;

	public MachineProductsCollection AutolatheProducts { get => autolatheProducts; }

	public delegate void MaterialsManipulating();

	public static event MaterialsManipulating MaterialsManipulated;

	private ItemTrait insertedMaterialType = null;
	private IEnumerator currentProduction = null;

	public enum AutolatheState
	{
		Idle,
		AcceptingMaterials,
		Production,
	}

	public override void OnStartClient()
	{
		SyncSprite(AutolatheState.Idle, AutolatheState.Idle);
		base.OnStartClient();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		EnsureInit();
	}

	private void Awake()
	{
		EnsureInit();
	}

	public void EnsureInit()
	{
		SyncSprite(AutolatheState.Idle, AutolatheState.Idle);
	}

	public void OnEnable()
	{
		registerObject = GetComponent<RegisterObject>();
	}

	private void UpdateGUI()
	{
		//Delegate calls method in all subscribers when material is changed
		if (MaterialsManipulated != null)
		{
			MaterialsManipulated();
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!interaction.HandSlot.IsEmpty)
		{
			//Checks if materialStorage has the materialRecord
			foreach (ItemTrait material in materialStorage.ItemTraitToMaterialRecord.Keys)
			{
				if (Validations.HasItemTrait(interaction.HandObject, material))
				{
					insertedMaterialType = material;
					return true;
				};
			}
		}

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Can't insert materials while exofab is in production.
		if (stateSync != AutolatheState.Production)
		{
			int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
			if (materialStorage.TryAddMaterialSheet(insertedMaterialType, materialSheetAmount))
			{
				Inventory.ServerDespawn(interaction.HandObject);
				if (stateSync == AutolatheState.Idle)
				{
					StartCoroutine(AnimateAcceptingMaterials());
				}
				UpdateGUI();
			}
			else Chat.AddActionMsgToChat(interaction.Performer, "Autolathe is full",
				"Autolathe is full");
		}
		else Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
			"Cannot accept materials while fabricating");
	}

	private IEnumerator AnimateAcceptingMaterials()
	{
		stateSync = AutolatheState.AcceptingMaterials;

		yield return WaitFor.Seconds(0.9f);
		if (stateSync == AutolatheState.Production)
		{
			//Do nothing if production was started during the material insertion animation
		}
		else
		{
			stateSync = AutolatheState.Idle;
		}
	}

	[Server]
	public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
	{
		if (materialStorage.TryRemoveMaterialSheet(materialType, amountOfSheets))
		{
			Spawn.ServerPrefab(materialStorage.ItemTraitToMaterialRecord[materialType].materialPrefab,
				registerObject.WorldPositionServer, transform.parent, count: amountOfSheets);

			UpdateGUI();
		}
	}

	[Server]
	public bool CanProcessProduct(MachineProduct product)
	{
		if (materialStorage.TryRemoveCM3Materials(product.materialToAmounts))
		{
			if (APCPoweredDevice.IsOn(PoweredState))
			{
				currentProduction = ProcessProduction(product.Product, product.ProductionTime);
				StartCoroutine(currentProduction);
				return true;
			}
		}

		return false;
	}

	private IEnumerator ProcessProduction(GameObject productObject, float productionTime)
	{
		stateSync = AutolatheState.Production;
		yield return WaitFor.Seconds(productionTime);

		Spawn.ServerPrefab(productObject, registerObject.WorldPositionServer, transform.parent, count: 1);
		stateSync = AutolatheState.Idle;
	}

	[Server]
	public void DropAllMaterials()
	{
		foreach (MaterialRecord materialRecord in materialStorage.ItemTraitToMaterialRecord.Values)
		{
			GameObject materialToSpawn = materialRecord.materialPrefab;
			int sheetsPerCM3 = materialStorage.CM3PerSheet;
			int amountToSpawn = materialRecord.CurrentAmount / sheetsPerCM3;

			if (amountToSpawn > 0)
			{
				Spawn.ServerPrefab(materialToSpawn, gameObject.transform.position, transform.parent, count: amountToSpawn);
			}
		}
	}

	public void SyncSprite(AutolatheState stateOld, AutolatheState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == AutolatheState.Idle)
		{
			spriteHandler.SetSpriteSO(idleSprite);
		}
		else if (stateNew == AutolatheState.Production)
		{
			spriteHandler.SetSpriteSO(productionSprite);
		}
		else if (stateNew == AutolatheState.AcceptingMaterials)
		{
			spriteHandler.SetSpriteSO(acceptingMaterialsSprite);
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		if (currentProduction != null)
		{
			StopCoroutine(currentProduction);
			currentProduction = null;
		}

		DropAllMaterials();
	}

	public void PowerNetworkUpdate(float Voltage) { }

	public void StateUpdate(PowerStates State)
	{
		PoweredState = State;
	}
}