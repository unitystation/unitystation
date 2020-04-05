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

	[Space]
	[SerializeField] private ItemTrait silver;

	[SerializeField] private ItemTrait gold;
	[SerializeField] private ItemTrait titanium;
	[SerializeField] private ItemTrait diamond;
	[SerializeField] private ItemTrait bluespace;
	[SerializeField] private ItemTrait bananium;
	[SerializeField] private ItemTrait uranium;
	[SerializeField] private ItemTrait plastic;

	[Space]
	[SerializeField] private GameObject metalPrefab;

	[SerializeField] private GameObject glassPrefab;
	[SerializeField] private GameObject silverPrefab;
	[SerializeField] private GameObject goldPrefab;
	[SerializeField] private GameObject titaniumPrefab;
	[SerializeField] private GameObject plasmaPrefab;
	[SerializeField] private GameObject diamondPrefab;

	//[SerializeField] private GameObject bluespacePrefab;    not implemented in game yet
	[SerializeField] private GameObject bananiumPrefab;

	[SerializeField] private GameObject uraniumPrefab;
	//[SerializeField] private GameObject plasticPrefab;      not implemented in game yet

	private Dictionary<ItemTrait, GameObject> materialTypeByPrefab;

	private List<ItemTrait> acceptableMaterials;
	private ItemTrait materialType;
	public int ironAmount { get; set; }
	public int glassAmount { get; set; }
	public int silverAmount { get; set; }
	public int plasmaAmount { get; set; }
	public int uraniumAmount { get; set; }
	public int goldAmount { get; set; }
	public int titaniumAmount { get; set; }
	public int diamondAmount { get; set; }
	public int bananiumAmount { get; set; }
	public int plasticAmount { get; set; }
	//bluespace crystals are not implemented yet
	//public int bluespaceSheetAmount { get; set; }

	public delegate void MaterialsManipulating();

	public static event MaterialsManipulating MaterialsManipulated;

	private void UpdateGUI()
	{
		// Change event runs updateAll in ChemistryGUI
		if (MaterialsManipulated != null)
		{
			Logger.Log("Calling delegate");
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
		acceptableMaterials = new List<ItemTrait>(){
		CommonTraits.Instance.MetalSheet, CommonTraits.Instance.GlassSheet,
		CommonTraits.Instance.SolidPlasma, gold, titanium, diamond, bluespace,
		bananium, uranium, plastic
	};
		materialTypeByPrefab = new Dictionary<ItemTrait, GameObject>()
		{
			{ CommonTraits.Instance.MetalSheet, metalPrefab },
			{ CommonTraits.Instance.GlassSheet, glassPrefab },
			{ CommonTraits.Instance.SolidPlasma, plasmaPrefab },
			{ silver, silverPrefab },
			{ gold, goldPrefab },
			{ titanium, titaniumPrefab},
			{ uranium, uraniumPrefab },
			{ diamond, diamondPrefab },
			//{ bananium, bananiumPrefab } not implemented in game yet
			//{ bluespace, bluespacePrefab },   not implemented in game yet
			//{ plastic, plasticPrefab },   not implemented in game yet
		};
		stateSync = ExosuitFabricatorState.Idle;
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
			foreach (ItemTrait material in acceptableMaterials)
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

			Inventory.ServerDespawn(interaction.HandObject);
			UpdateMaterialCount(materialSheetAmount * 2000, materialType);
			materialStorage.TryAddMaterialSheet(materialType, materialSheetAmount);
		}
	}

	//Dispenses material sheets if there's enough
	public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
	{
		if (materialTypeByPrefab.ContainsKey(materialType))
		{
			if (HasMaterialAmount(amountOfSheets, materialType))
			{
				UpdateMaterialCount(-amountOfSheets * 2000, materialType);
				materialStorage.TryRemoveMaterialSheet(materialType, amountOfSheets);
				Spawn.ServerPrefab(materialTypeByPrefab[materialType],
				registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: amountOfSheets);
			}
			else
			{
				Logger.Log("Not enough materials!!!");
			}
		}
	}

	public bool HasMaterialAmount(int amountOfSheets, ItemTrait materialType)
	{
		if (materialType == CommonTraits.Instance.MetalSheet) return ironAmount >= amountOfSheets * 2000;
		else if (materialType == CommonTraits.Instance.GlassSheet) return glassAmount >= amountOfSheets * 2000;
		else if (materialType == CommonTraits.Instance.SolidPlasma) return plasmaAmount >= amountOfSheets * 2000;
		else if (materialType == silver) return silverAmount >= amountOfSheets * 2000;
		else if (materialType == gold) return goldAmount >= amountOfSheets * 2000;
		else if (materialType == titanium) return goldAmount >= amountOfSheets * 2000;
		else if (materialType == uranium) return uraniumAmount >= amountOfSheets * 2000;
		else if (materialType == diamond) return diamondAmount >= amountOfSheets * 2000;
		//else if (sheetType == bluespace) { } Material not implemented yet
		else if (materialType == bananium) return bananiumAmount >= amountOfSheets * 2000;
		else if (materialType == plastic) return plasticAmount >= amountOfSheets * 2000;
		else return false;
	}

	//Updates the amount of sheets in the exofab
	public void UpdateMaterialCount(int materialAmount, ItemTrait sheetType)
	{
		if (sheetType == CommonTraits.Instance.MetalSheet) ironAmount += materialAmount;
		else if (sheetType == CommonTraits.Instance.GlassSheet) glassAmount += materialAmount;
		else if (sheetType == CommonTraits.Instance.SolidPlasma) plasmaAmount += materialAmount;
		else if (sheetType == silver) silverAmount += materialAmount;
		else if (sheetType == gold) goldAmount += materialAmount;
		else if (sheetType == titanium) goldAmount += materialAmount;
		else if (sheetType == uranium) uraniumAmount += materialAmount;
		else if (sheetType == diamond) diamondAmount += materialAmount;
		//else if (sheetType == bluespace) { } Material Not implemented yet
		else if (sheetType == bananium) bananiumAmount += materialAmount;
		else if (sheetType == plastic) plasticAmount += materialAmount;
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