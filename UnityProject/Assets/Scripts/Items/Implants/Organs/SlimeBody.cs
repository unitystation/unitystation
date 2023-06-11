using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using Util;

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

		var IsAdult = ReagentCirculatedComponent.AssociatedSystem.BloodPool[SlimeJelly] > AdultThreshold;

		if (IsAdult != CurrentState)
		{
			SetCurrentState(IsAdult);
		}


	}

	public void SetCurrentState(bool State)
	{
		if (State)
		{
			foreach (var Sprite in RelatedPart.RelatedPresentSprites)
			{
				Sprite.baseSpriteHandler.SetSpriteSO(AdultSprite);
			}
		}
		else
		{
			foreach (var Sprite in RelatedPart.RelatedPresentSprites)
			{
				Sprite.baseSpriteHandler.SetSpriteSO(BabySprite);
			}
		}

		CurrentState = State;
	}

}




