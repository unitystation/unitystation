using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Main component for the exosuit fabricator.
/// </summary>
public class ExosuitFabricator : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IServerDespawn
{
	[SyncVar(hook = nameof(SyncSprite))]
	private ExosuitFabricatorState stateSync;

	[SerializeField] private SpriteHandler spriteHandler = null;
	[SerializeField] private SpriteDataSO idleSprite = null;
	[SerializeField] private SpriteDataSO acceptingMaterialsSprite = null;
	[SerializeField] private SpriteDataSO productionSprite = null;
	private RegisterObject registerObject = null;
	public MaterialStorage materialStorage = null;
	public MachineProductsCollection exoFabProducts = null;
	private ItemTrait InsertedMaterialType = null;
	private IEnumerator currentProduction = null;

	public delegate void MaterialsManipulating();

	public static event MaterialsManipulating MaterialsManipulated;

	private void UpdateGUI()
	{
		//Delegate calls method in all subscribers when material is changed
		if (MaterialsManipulated != null)
		{
			MaterialsManipulated();
		}
	}

	public enum ExosuitFabricatorState
	{
		Idle,
		AcceptingMaterials,
		Production,
	};

	public override void OnStartClient()
	{
		SyncSprite(ExosuitFabricatorState.Idle, ExosuitFabricatorState.Idle);
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
		SyncSprite(ExosuitFabricatorState.Idle, ExosuitFabricatorState.Idle);
		materialStorage = this.GetComponent<MaterialStorage>();
	}

	public void OnEnable()
	{
		registerObject = GetComponent<RegisterObject>();
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
					InsertedMaterialType = material;
					return true;
				};
			}
		}

		return false;
	}

	//Clicking the exofab with material sheets(Metal sheets, glass sheets, silver sheets, etc.)
	//in hand will insert the materials in the storage and update the GUI.
	//Every sheet is 2000cm^3

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Can't insert materials while exofab is in production.
		if (stateSync != ExosuitFabricatorState.Production)
		{
			int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
			if (materialStorage.TryAddMaterialSheet(InsertedMaterialType, materialSheetAmount))
			{
				Inventory.ServerDespawn(interaction.HandObject);
				if (stateSync == ExosuitFabricatorState.Idle)
				{
					StartCoroutine(AnimateAcceptingMaterials());
				}
				UpdateGUI();
			}
			else Chat.AddActionMsgToChat(interaction.Performer, "Exosuit Fabricator is full",
				"Exosuit Fabricator is full");
		}
		else Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
			"Cannot accept materials while fabricating");
	}

	private IEnumerator AnimateAcceptingMaterials()
	{
		stateSync = ExosuitFabricatorState.AcceptingMaterials;

		yield return WaitFor.Seconds(1.2f);
		if (stateSync == ExosuitFabricatorState.Production)
		{
			//Do nothing if production was started during the material insertion animation
		}
		else
		{
			stateSync = ExosuitFabricatorState.Idle;
		}
	}

	public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
	{
		if (materialStorage.TryRemoveMaterialSheet(materialType, amountOfSheets))
		{
			Spawn.ServerPrefab(materialStorage.ItemTraitToMaterialRecord[materialType].materialPrefab,
			registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: amountOfSheets);

			UpdateGUI();
		}
		else
		{
			//Not enough materials to dispense
		}
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
				Spawn.ServerPrefab(materialToSpawn, registerObject.WorldPositionServer, transform.parent, count: amountToSpawn);
			}
		}
	}

	/// <summary>
	/// Checks the material storage to see if there's enough materials and if true will process the product
	/// </summary>
	public bool CanProcessProduct(MachineProduct product)
	{
		if (materialStorage.TryRemoveCM3Materials(product.materialToAmounts))
		{
			currentProduction = ProcessProduction(product.Product, product.ProductionTime);
			StartCoroutine(currentProduction);
			return true;
		}

		return false;
	}

	private IEnumerator ProcessProduction(GameObject productObject, float productionTime)
	{
		stateSync = ExosuitFabricatorState.Production;
		yield return WaitFor.Seconds(productionTime);

		Spawn.ServerPrefab(productObject, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: 1);
		stateSync = ExosuitFabricatorState.Idle;
	}

	public void SyncSprite(ExosuitFabricatorState stateOld, ExosuitFabricatorState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == ExosuitFabricatorState.Idle)
		{
			spriteHandler.SetSpriteSO(idleSprite);
		}
		else if (stateNew == ExosuitFabricatorState.Production)
		{
			spriteHandler.SetSpriteSO(productionSprite);
		}
		else if (stateNew == ExosuitFabricatorState.AcceptingMaterials)
		{
			spriteHandler.SetSpriteSO(acceptingMaterialsSprite);
		}
		else
		{
			//Do nothing
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
}