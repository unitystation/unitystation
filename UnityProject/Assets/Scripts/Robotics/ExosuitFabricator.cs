using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

	[Space]
	//List of acceptable materials requires bluespace crystal trait
	[SerializeField] private ItemTrait silver;

	[SerializeField] private ItemTrait gold;
	[SerializeField] private ItemTrait titanium;
	[SerializeField] private ItemTrait diamond;
	[SerializeField] private ItemTrait bluespace;
	[SerializeField] private ItemTrait bananium;
	[SerializeField] private ItemTrait uranium;
	[SerializeField] private ItemTrait plastic;
	private List<ItemTrait> acceptableMaterials;
	private ItemTrait materialType;
	public int metalSheetAmount { get; set; }
	public int glassSheetAmount { get; set; }
	public int silverSheetAmount { get; set; }
	public int plasmaSheetAmount { get; set; }
	public int uraniumSheetAmount { get; set; }
	public int goldSheetAmount { get; set; }
	public int titaniumSheetAmount { get; set; }
	public int diamondSheetAmount { get; set; }
	public int bananiumSheetAmount { get; set; }
	public int plasticSheetAmount { get; set; }
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
	//will insert the materials in the storage and update the GUI.
	public void ServerPerformInteraction(HandApply interaction)
	{
		//Can't insert materials while exofab is under production.

		if (stateSync != ExosuitFabricatorState.Production)
		{
			int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
			Logger.Log("Current metal: " + metalSheetAmount);

			Inventory.ServerDespawn(interaction.HandObject);
			UpdateMaterialCount(materialSheetAmount, materialType);
			Logger.Log("Metal After Transfer and Update: " + metalSheetAmount);
		}
	}

	//Updates the amount of sheets in the exofab
	private void UpdateMaterialCount(int amountOfSheets, ItemTrait metalType)
	{
		if (metalType == CommonTraits.Instance.MetalSheet) metalSheetAmount += amountOfSheets;
		else if (metalType == CommonTraits.Instance.GlassSheet) glassSheetAmount += amountOfSheets;
		else if (metalType == CommonTraits.Instance.SolidPlasma) plasmaSheetAmount += amountOfSheets;
		else if (metalType == silver) silverSheetAmount += amountOfSheets;
		else if (metalType == gold) goldSheetAmount += amountOfSheets;
		else if (metalType == titanium) goldSheetAmount += amountOfSheets;
		else if (metalType == uranium) uraniumSheetAmount += amountOfSheets;
		else if (metalType == diamond) diamondSheetAmount += amountOfSheets;
		else if (metalType == bluespace) { }//Not implemented yet
		else if (metalType == bananium) bananiumSheetAmount += amountOfSheets;
		else if (metalType == plastic) plasticSheetAmount += amountOfSheets;
		UpdateGUI();
		Logger.Log("Materials being updated");
	}

	//Needs product to be produced and time it takes to be produced.
	private IEnumerator ProcessProduction()
	{
		yield return WaitFor.Seconds(5f);

		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: 1);
		stateSync = ExosuitFabricatorState.Idle;

		Debug.Log("Production End");
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