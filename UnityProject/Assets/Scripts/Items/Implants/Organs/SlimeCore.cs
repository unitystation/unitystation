using System.Collections;
using System.Collections.Generic;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

[RequireComponent( typeof(ReagentCirculatedComponent))]
public class SlimeCore : BodyPartFunctionality
{

	public SpriteDataSO AdultSprite;
	public SpriteDataSO BabySprite;

	private ReagentCirculatedComponent ReagentCirculatedComponent;

	public float StartingAmount = 25;

	public override void Awake()
	{
		base.Awake();
		ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
	}


	public SpriteDataSO SpriteDataSO;

	[NaughtyAttributes.Button()]

	public void InitialiseBabySlime()
	{
		var Multiplier = (StartingAmount / ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total);
		ReagentCirculatedComponent.AssociatedSystem.BloodPool.Multiply(Multiplier);
		//set Blood level
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



}
