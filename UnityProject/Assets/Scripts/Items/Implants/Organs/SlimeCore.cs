using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Implants.Organs;
using Systems.Character;
using UI.CharacterCreator;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent( typeof(ReagentCirculatedComponent))]
public class SlimeCore : BodyPartFunctionality
{


	public int DropDownIndex = 0;

	private CharacterSheet ForCore =>
		new CharacterSheet()
		{
			Species = "SlimeBall",
			SerialisedBodyPartCustom = new List<CustomisationStorage>()
			{
				new CustomisationStorage()
				{
					path = "/SlimeBody/SlimeCore" ,
					Data = (DropDownIndex + 1).ToString()
				}

			}
		};

	public SpriteDataSO AdultSprite;
	public SpriteDataSO BabySprite;

	private ReagentCirculatedComponent ReagentCirculatedComponent;


	public List<ChanceAndPrefab> CanSplitInto = new List<ChanceAndPrefab>();


	public Reagent SlimeJelly;


	public float StartingAmount = 25;


	public int CurrentNumberOfCore = 1;

	public bool Stabilised = false;
	public bool DeStabilised = false;


	public bool Enhanced = false;
	public bool EnhancedUsedUp = false;

	public bool MakeIntoBabySlimeWhenAddedIfSlime = true;
	private bool BabySlimeInit = false;

	public override void Awake()
	{
		base.Awake();
		ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();

	}


	public void Start()
	{
		InitialiseBabySlime();
	}


	[System.Serializable]
	public class ChanceAndPrefab
	{
		[Range(0, 100)] public int ChanceToMutateTo;
		public SlimeCore CoreMutateTo;
	}

	[NaughtyAttributes.Button()]

	public void InitialiseBabySlime()
	{
		if (RelatedPart.HealthMaster == null) return;
		if (BabySlimeInit) return;
		BabySlimeInit = true;
		var Multiplier = (StartingAmount / ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total);
		ReagentCirculatedComponent.AssociatedSystem.BloodPool.Multiply(Multiplier);

		foreach (var bodyPart in RelatedPart.HealthMaster.BodyPartList)
		{
			var Body =  bodyPart.GetComponent<SlimeBody>();

			if (Body != null)
			{
				Body.BabySprite = BabySprite;
				Body.AdultSprite = AdultSprite;
				Body.CurrentState = null;
			}

		}
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{

		foreach (var bodyPart in livingHealth.BodyPartList)
		{

			var Body =  bodyPart.GetComponent<SlimeBody>();

			if (Body != null)
			{
				Body.BabySprite = BabySprite;
				Body.AdultSprite = AdultSprite;
				Body.CurrentState = null;
			}

		}
	} //Warning only add body parts do not remove body parts in this

	[NaughtyAttributes.Button()]
	public void DEBUGSlimesSplit()
	{
		var Multiplier =  (100f / ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total);
		ReagentCirculatedComponent.AssociatedSystem.BloodPool.Multiply(Multiplier);
		SlimesSplit();
	}

	public bool CanSlimesSplit()
	{
		return ReagentCirculatedComponent.AssociatedSystem.BloodPool[SlimeJelly] > 99;
	}

	public void SlimesSplit()
	{

		if (CanSlimesSplit())
		{

			// Loop four times to create four offspring
			for (int i = 0; i < 4; i++)
			{
				// Generate a random number between 0 and 100
				int randomNumber = Random.Range(0, 100);

				// Find the index of the chosen prefab based on the percentage chances
				int chosenIndex = GetChosenIndex(randomNumber);

				SlimeCore NewCore = CanSplitInto[chosenIndex].CoreMutateTo;

				if (CanSplitInto[chosenIndex].CoreMutateTo == null)
				{
					//is it Self
					NewCore = this;
				}

				// Instantiate a new offspring based on the chosen prefab
				var Mind = PlayerSpawn.NewSpawnCharacterV2(null, NewCore.ForCore, true);
				Mind.Body.ObjectPhysics.AppearAtWorldPositionServer(RelatedPart.HealthMaster.gameObject.AssumedWorldPosServer());

				var core = Mind.Body.GetComponent<LivingHealthMasterBase>().brain.GetComponent<SlimeCore>();
				core.InitialiseBabySlime();

				var GoodPlayers = core.GetComponent<BrainSlime>();
				GoodPlayers.GoodPlayers = GoodPlayers.GoodPlayers.Union(this.GetComponent<BrainSlime>().GoodPlayers).ToDictionary(s => s.Key, s => s.Value);

				core.Stabilised = this.Stabilised;

			}


			_ = Despawn.ServerSingle(RelatedPart.HealthMaster.gameObject);
		}


	}

	// Helper method to get the index of the chosen prefab based on the percentage chances
    private int GetChosenIndex(int randomNumber)
    {
        int totalChances = 0;

        // Calculate the total chances
        for (int i = 0; i < CanSplitInto.Count; i++)
        {
	        if (CanSplitInto[i].CoreMutateTo == null)
	        {
		        if (this.Stabilised)
		        {
			        totalChances += Mathf.RoundToInt(CanSplitInto[i].ChanceToMutateTo * 1.15f);
		        }
		        else
		        {
			        totalChances += CanSplitInto[i].ChanceToMutateTo;
		        }

	        }
	        else
	        {
		        if (this.DeStabilised)
		        {
			        totalChances += Mathf.RoundToInt(CanSplitInto[i].ChanceToMutateTo * 1.12f);
		        }
		        else
		        {
			        totalChances += CanSplitInto[i].ChanceToMutateTo;
		        }


	        }

        }

        int cumulativeChance = 0;

        // Loop through the percentage chances
        for (int i = 0; i < CanSplitInto.Count; i++)
        {
            cumulativeChance += CanSplitInto[i].ChanceToMutateTo;

            // Check if the random number falls within the cumulative chance range
            if (randomNumber < (cumulativeChance * 100 / totalChances))
            {
                return i; // Return the index of the chosen prefab
            }
        }

        // If no index is found, return the last index
        return CanSplitInto.Count - 1;
    }
}
