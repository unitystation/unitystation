using System.Collections.Generic;
using UnityEngine;
using Mirror;

//Used when spawning the food
[RequireComponent(typeof(CustomNetTransform))]
[DisallowMultipleComponent]
public class GrownFood : NetworkBehaviour, IInteractable<HandActivate>
{
	public PlantData plantData;
	public ReagentContainer reagentContainer;
	private GameObject SeedPacket;
	private SpriteRenderer SpriteSizeAdjustment;
	private SpriteHandler Sprite;
	private ItemAttributesV2 ItemAttributesV2;




	[SyncVar(hook = nameof(SyncSize))]
	public float SizeScale;

	public void SyncSize(float oldScale, float newScale)
	{
		SizeScale = newScale;
		SpriteSizeAdjustment.transform.localScale = new Vector3((SizeScale), (SizeScale), (SizeScale));
	}

	private void Awake()
	{
		Sprite.PushTexture();
	}

	public override void OnStartClient()
	{
		SyncSize(this.SizeScale, this.SizeScale);
		base.OnStartClient();
	}

	/// <summary>
	/// Called when plant creates food
	/// </summary>
	public void SetUpFood(PlantData newPlantData)
	{
		plantData.SetValues(newPlantData);
		SyncSize(SizeScale, 0.5f + (newPlantData.Potency / 100f));
	}

	/// <summary>
	/// Gets seeds for plant and replaces held food with seeds
	/// DOES NOT WORK, eating overrides this.
	/// </summary>
	/// <param name="interaction"></param>
	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (plantData != null)
		{
			var seedObject = Spawn.ServerPrefab(SeedPacket, interaction.Performer.RegisterTile().WorldPositionServer, parent: interaction.Performer.transform.parent).GameObject;
			var seedPacket = seedObject.GetComponent<SeedPacket>();
			seedPacket.plantData = new PlantData();
			seedPacket.plantData.SetValues(plantData);

			seedPacket.SyncPlant(null, plantData.Name);

			var slot = interaction.HandSlot;
			Inventory.ServerAdd(seedObject, interaction.HandSlot, ReplacementStrategy.DespawnOther);
		}


	}
}

