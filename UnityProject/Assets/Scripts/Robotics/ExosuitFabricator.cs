using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//

/// <summary>
/// Main component for the exosuit fabricator.
/// </summary>
public class ExosuitFabricator : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SyncVar(hook = nameof(SyncSprite))]
	private ExosuitFabricatorState stateSync;

	[SerializeField] private SpriteHandler spriteHandler;
	[SerializeField] private SpriteSheetAndData idleSprite;
	[SerializeField] private SpriteSheetAndData productionSprite;
	private RegisterObject registerObject;
	public MaterialStorage materialStorage;

	private ItemTrait materialType;

	public delegate void MaterialsManipulating();

	public static event MaterialsManipulating MaterialsManipulated;

	private void UpdateGUI()
	{
		// Change event runs updateAll in ChemistryGUI
		if (MaterialsManipulated != null)
		{
			MaterialsManipulated();
		}
	}

	public enum ExosuitFabricatorState
	{
		Idle,
		Production,
	};

	private void Awake()
	{
		stateSync = ExosuitFabricatorState.Idle;
		materialStorage = this.GetComponent<MaterialStorage>();
	}

	public void OnEnable()
	{
		registerObject = GetComponent<RegisterObject>();
	}

	public override void OnStartClient()
	{
		SyncSprite(stateSync, stateSync);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//Checks if the material is acceptable and sets the current materialType
		if (!interaction.HandSlot.IsEmpty)
		{
			//Checks if materialStorage has the materialRecord
			foreach (ItemTrait material in materialStorage.ItemTraitToMaterialRecord.Keys)
			{
				if (Validations.HasItemTrait(interaction.HandObject, material))
				{
					materialType = material;
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
		//Can't insert materials while exofab is under production.

		if (stateSync != ExosuitFabricatorState.Production)
		{
			int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
			if (materialStorage.TryAddMaterialSheet(materialType, materialSheetAmount))
			{
				Inventory.ServerDespawn(interaction.HandObject);
				UpdateGUI();
			}
			else Logger.Log("materialStorage is full");
		}
		else Logger.Log("Cannot put in materials while producing");
	}

	//Dispenses material sheets if there's enough
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
			Logger.Log("Not enough materials!!!");
		}
	}

	//Updates the amount of sheets in the exofab
	public void UpdateMaterialSheetCount(int materialAmount, ItemTrait sheetType)
	{
		if (materialStorage.ItemTraitToMaterialRecord.ContainsKey(sheetType))
		{
			materialStorage.TryAddMaterialSheet(sheetType, materialAmount);
		}
		UpdateGUI();
	}

	//Needs product to be produced and time it takes to be produced.
	private IEnumerator ProcessProduction()
	{
		yield return WaitFor.Seconds(5f);

		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: 1);
		stateSync = ExosuitFabricatorState.Idle;
	}

	public void SyncSprite(ExosuitFabricatorState stateOld, ExosuitFabricatorState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == ExosuitFabricatorState.Idle)
		{
			spriteHandler.SetSprite(idleSprite);
		}
		else if (stateNew == ExosuitFabricatorState.Production)
		{
			spriteHandler.SetSprite(productionSprite, 0);
		}
		else
		{
			//Do nothing
		}
	}
}