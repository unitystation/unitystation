using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Core;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Food;
using UnityEngine;

[RequireComponent( typeof(ReagentCirculatedComponent))]
public class SlimeEat : BodyPartFunctionality
{
	public bool CurrentlyEating => EdibleCurrentlyEating || CurrentlyBuckledToObject;
	public UniversalObjectPhysics EdibleCurrentlyEating => RelatedPart.HealthMaster.OrNull()?.ObjectBehaviour.OrNull()?.ObjectIsBuckling;
	public LivingHealthMasterBase LivingHealthMasterBaseCurrentlyEating => CurrentlyBuckledToObject.OrNull()?.GetComponent<LivingHealthMasterBase>();

	private UniversalObjectPhysics CurrentlyBuckledToObject => RelatedPart.HealthMaster.OrNull()?.ObjectBehaviour.OrNull()?.BuckledToObject;

	public GameObject DUBUGCurrentlyEating;


	public Reagent SlimeJelly;
	public Reagent Nutriment;

	private ReagentCirculatedComponent ReagentCirculatedComponent;

	public override void Awake()
	{
		base.Awake();
		ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
	}

	[NaughtyAttributes.Button()]
	public void ClimbAndEatDUBUG()
	{
		ClimbAndEat(DUBUGCurrentlyEating);
	}


	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
	{
		if (CurrentlyEating)
		{

			if (LivingHealthMasterBaseCurrentlyEating)
			{
				LivingHealthMasterBaseCurrentlyEating.ObjectBehaviour.Unbuckle();
			}


			if (EdibleCurrentlyEating)
			{
				livingHealth.ObjectBehaviour.Unbuckle();
			}
		}
	}

	public void ClimbAndEat(GameObject ToEat)
	{
		if (ToEat.GetComponent<UniversalObjectPhysics>().IsVisible == false) return;
		if (RelatedPart.HealthMaster.GetComponent<UniversalObjectPhysics>().IsVisible == false) return;
		if ((this.RelatedPart.HealthMaster.transform.position - ToEat.transform.position).magnitude > 1.5)
		{
			return;
		}

		if ( RelatedPart.HealthMaster.ObjectBehaviour.BuckledToObject != null || RelatedPart.HealthMaster.ObjectBehaviour.ObjectIsBuckling != null ) return;


		var Edible = ToEat.GetComponent<Edible>();

		if (Edible != null)
		{
			//Assume is an item
			RelatedPart.HealthMaster.ObjectBehaviour.BuckleTo(ToEat.GetComponent<UniversalObjectPhysics>());
			return;
		}

		var Health = ToEat.GetComponent<LivingHealthMasterBase>();

		if (Health != null)
		{
			if (Health.ObjectBehaviour.BuckledToObject != null || Health.ObjectBehaviour.ObjectIsBuckling != null ) return; //just buckle yourself to a chair lol

			if (Health.IsDead)
			{
				return;
			}

			//Player/mob
			ToEat.GetComponent<UniversalObjectPhysics>().BuckleTo( RelatedPart.HealthMaster.ObjectBehaviour);
			return;
		}
	}


	public void StopEating()
	{
		if (LivingHealthMasterBaseCurrentlyEating != null)
		{
			if (RelatedPart.HealthMaster.ObjectBehaviour.BuckledToObject != null)
			{
				RelatedPart.HealthMaster.ObjectBehaviour.BuckledToObject.Unbuckle();
			}
		}

		if (EdibleCurrentlyEating != null)
		{
			RelatedPart.HealthMaster.ObjectBehaviour.Unbuckle();
		}

	}

	[NaughtyAttributes.Button()]
	public void FullEat()
	{
		ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(SlimeJelly, 150f);
	}

	public override void ImplantPeriodicUpdate()
	{
		if (RelatedPart.HealthMaster.ObjectBehaviour.IsVisible == false) return;

		if (CurrentlyEating == false) return;


		//TODO check buckling instead of this!!!
		//Problem is determining intent, Since you can be riding something without eating it, So you have to set here
		//Also the get component for edible and Living health , Could be fixed by common component reference in universal object physics
		if (EdibleCurrentlyEating != null && EdibleCurrentlyEating.GetComponent<Edible>() != null)
		{
			var food = EdibleCurrentlyEating.GetComponent<Edible>().GetMixForBite(null);
			var Nutriments =  food[Nutriment];
			food.Remove(Nutriment, Nutriments);
			food.Add(SlimeJelly,Nutriments);
			ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(food);
		}

		if (LivingHealthMasterBaseCurrentlyEating != null)
		{
			if (LivingHealthMasterBaseCurrentlyEating.IsDead)
			{
				StopEating();
				return;
			}

			LivingHealthMasterBaseCurrentlyEating.ApplyDamageAll(RelatedPart.HealthMaster.gameObject, 1.33332f, AttackType.Bio,DamageType.Clone, true);
			LivingHealthMasterBaseCurrentlyEating.ApplyDamageAll(RelatedPart.HealthMaster.gameObject, 1.33332f, AttackType.Bio,DamageType.Burn, true);
			ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(SlimeJelly, 0.58f);
		}

	}
}
