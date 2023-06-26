using System;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace Items.Implants.Organs
{
	[RequireComponent( typeof(ReagentCirculatedComponent))]
    public class SlimeBody : BodyPartFunctionality
    {

    	[NonSerialized] public SpriteDataSO AdultSprite;
    	[NonSerialized] public SpriteDataSO BabySprite;

    	private ReagentCirculatedComponent ReagentCirculatedComponent;

    	public Reagent SlimeJelly;

    	public float AdultThreshold = 50;

    	public bool? CurrentState;

    	public override void Awake()
    	{
    		base.Awake();
    		ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
    	}

    	public override void ImplantPeriodicUpdate()
    	{
    		if (CurrentState == null)
    		{
    			SetCurrentState(ReagentCirculatedComponent.AssociatedSystem.BloodPool[SlimeJelly] > AdultThreshold);
    		}

    		var isAdult = ReagentCirculatedComponent.AssociatedSystem.BloodPool[SlimeJelly] > AdultThreshold;

    		if (isAdult != CurrentState)
    		{
    			SetCurrentState(isAdult);
    		}


    	}

    	public void SetCurrentState(bool adult)
    	{
    		if (adult)
    		{
    			foreach (var sprite in RelatedPart.RelatedPresentSprites)
    			{
    				sprite.baseSpriteHandler.SetSpriteSO(AdultSprite);
    			}
    		}
    		else
    		{
    			foreach (var Sprite in RelatedPart.RelatedPresentSprites)
    			{
    				Sprite.baseSpriteHandler.SetSpriteSO(BabySprite);
    			}
    		}

    		CurrentState = adult;
    	}

    }





}
