using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Chemistry.Components;

//Used when spawning the food
[RequireComponent(typeof(CustomNetTransform))]
[RequireComponent(typeof(ReagentContainer))]
[DisallowMultipleComponent]
public class GrownFood : NetworkBehaviour
{
	[SerializeField]
	private PlantData plantData;


	public ReagentContainer reagentContainer;
	public Chemistry.Reagent nutrient;
	public GameObject SeedPacket => seedPacket;

	[SerializeField]
	public GameObject seedPacket = null;
	[SerializeField]
	private SpriteRenderer SpriteSizeAdjustment = null;
	[SerializeField]
	private SpriteHandler Sprite;
	[SerializeField]
	private Edible edible;

	[SyncVar(hook = nameof(SyncSize))]
	public float SizeScale;

	public void SyncSize(float oldScale, float newScale)
	{
		SizeScale = newScale;
		SpriteSizeAdjustment.transform.localScale = new Vector3((SizeScale), (SizeScale), (SizeScale));
	}


	public PlantData GetPlantData()
	{
		PlantData _plantData = null;
		if (plantData.FullyGrownSpriteSO == null)
		{
			_plantData = SeedPacket.GetComponent<SeedPacket>().plantData;
		}
		else
		{
			_plantData = plantData;
		}

		return _plantData;
	}

	/*private void Awake()
	{
		if (SpriteSizeAdjustment.sprite.texture == null)
		{
			Debug.LogError("Attempted awake on food, failed to find texture", this);
			return;
		}
		var spritesheet = new SpriteSheetAndData { Texture = SpriteSizeAdjustment.sprite.texture };
		spritesheet.setSprites();
		Sprite.spriteData = SpriteFunctions.SetupSingleSprite(spritesheet);
		Sprite.PushTexture();
	}*/

	public override void OnStartClient()
	{
		SyncSize(this.SizeScale, this.SizeScale);
		base.OnStartClient();
	}

	/// <summary>
	/// Called when plant creates food
	/// </summary>
	public void SetUpFood(PlantData newPlantData, PlantTrayModification modification)
	{
		plantData = PlantData.MutateNewPlant(newPlantData, modification);
		SyncSize(SizeScale, 0.5f + (newPlantData.Potency / 200f));
		SetupChemicalContents();
		if(edible != null)
		{
			SetupEdible();
		}
	}

	/// <summary>
	/// Takes initial values and scales them based on potency
	/// </summary>
	private void SetupChemicalContents()
	{
		reagentContainer.Multiply(plantData.Potency);
	}

	/// <summary>
	/// Set NutritionLevel to be equal to nuriment amount
	/// </summary>
	private void SetupEdible()
	{
		//DOES NOT WORK! DO NOT USE THIS!
		// edible.NutritionLevel = Mathf.FloorToInt(reagentContainer[nutrient]);
	}

}

