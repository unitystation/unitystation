using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Food;
using UnityEngine;

[RequireComponent( typeof(ReagentCirculatedComponent))]
public class SlimeEat : BodyPartFunctionality
{
	public GameObject CurrentlyEating;
	public Edible EdibleCurrentlyEating;
	public LivingHealthMasterBase LivingHealthMasterBaseCurrentlyEating;

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


	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		if (CurrentlyEating != null)
		{
			var Physics = livingHealth.ObjectBehaviour;
			var EatingPhysics = CurrentlyEating.GetComponent<UniversalObjectPhysics>();
			if (Physics.BuckledToObject == EatingPhysics)
			{
				Physics.Unbuckle();
			}

			if (Physics.ObjectIsBucklingChecked.Component == EatingPhysics)
			{
				EatingPhysics.Unbuckle();
			}
		}
	}

	public void ClimbAndEat(GameObject ToEat)
	{
		if ((this.RelatedPart.HealthMaster.transform.position - ToEat.transform.position).magnitude > 1.5)
		{
			return;
		}

		if ( RelatedPart.HealthMaster.ObjectBehaviour.BuckledToObject != null || RelatedPart.HealthMaster.ObjectBehaviour.ObjectIsBucklingChecked.HasComponent ) return;

		var Edible = ToEat.GetComponent<Edible>();

		if (Edible != null)
		{
			//Assume is an item
			EdibleCurrentlyEating = Edible;
			CurrentlyEating = ToEat;
			RelatedPart.HealthMaster.ObjectBehaviour.BuckleTo(ToEat.GetComponent<UniversalObjectPhysics>());
			return;
		}

		var Health = ToEat.GetComponent<LivingHealthMasterBase>();

		if (Health != null)
		{
			if (Health.IsDead)
			{
				return;
			}

			//Player/mob
			LivingHealthMasterBaseCurrentlyEating = Health;
			CurrentlyEating = ToEat;
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

			LivingHealthMasterBaseCurrentlyEating = null;
			CurrentlyEating = null;
		}

		if (EdibleCurrentlyEating != null)
		{
			RelatedPart.HealthMaster.ObjectBehaviour.Unbuckle();
			LivingHealthMasterBaseCurrentlyEating = null;
			EdibleCurrentlyEating = null;
		}

	}

	public override void ImplantPeriodicUpdate()
	{
		if (EdibleCurrentlyEating != null)
		{
			var food = EdibleCurrentlyEating.GetMixForBite(null);
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

			LivingHealthMasterBaseCurrentlyEating.ApplyDamageAll(RelatedPart.HealthMaster.gameObject, 3.33333f, AttackType.Internal,DamageType.Clone, true);
			ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(SlimeJelly, 1.25f);
		}

	}
}
