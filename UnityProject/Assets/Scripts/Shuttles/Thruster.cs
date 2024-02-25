using System;
using System.Collections;
using System.Collections.Generic;
using Objects.Atmospherics;
using Systems.Atmospherics;
using Systems.Pipes;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

public class Thruster : MonoPipe
{
	public MatrixMove RelatedMove;

	public Rotatable Rotatable;
	//TODO Swapping matrices

	public bool DEBUGForwardsThruster = false;

	public bool DEBUGRightThruster = false;

	public bool DEBUGLeftThruster = false;

	public ThrusterDirectionClassification ThisThrusterDirectionClassification;

	public float MaxMolesUseda = 1;
	public float TargetMolesUsed = 0;

	public float ThrusterMultiplier = 100;

	public float AThrusterUseMultiplier = 0.072975f;


	public float ThrustPower;

	public bool SelfPowered = false;

	public Vector3 WorldThrustDirectionAndMagnitude
	{
		get
		{
			//ThrustVector.normalized * ThrustPower;
			return (Rotatable.CurrentDirection.ToLocalVector3() * ThrustPower).DirectionLocalToWorld(registerTile.Matrix);
		}
	}


	public Vector3 LocalThrustDirectionAndMagnitude
	{
		get
		{
			//ThrustVector.normalized * ThrustPower;
			return  Rotatable.CurrentDirection.ToLocalVector3() * ThrustPower;
		}
	}

	public Vector2 LocalPosition
	{
		get
		{
			return transform.localPosition;
		}
	}

	public enum ThrusterDirectionClassification
	{
		Forwards,
		Right,
		Left,
		Backwards
	}


	public void OnDestroy()
	{
		if (RelatedMove != null)
		{
			RelatedMove.NetworkedMatrixMove.RemoveThruster(this);
		}
	}


	// Start is called before the first frame update
    void Start()
    {
	    RelatedMove = this.GetComponentInParent<MatrixMove>();
	    if (RelatedMove != null)
	    {
		    RelatedMove.NetworkedMatrixMove.AddThruster(this);
	    }
	    Rotatable = this.GetComponentCustom<Rotatable>();

    }

    public float GetReactionAmount(MixAndVolume reagentMix, ThrusterFuelReactions.ThrusterMixAndEffect ThrusterMixAndEffect)
    {
	    var reactionMultiplier = Mathf.Infinity;
	    foreach (var ingredient in ThrusterMixAndEffect.ChemicalMakeUp)
	    {
		    var value = reagentMix[ingredient] / ingredient.Amount;
		    if (value < reactionMultiplier)
		    {
			    reactionMultiplier = value;
		    }
	    }
	    return reactionMultiplier;
    }

    public override void TickUpdate()
    {
	    pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

	    if (SelfPowered)
	    {
		    ThrustPower = TargetMolesUsed  * ThrusterMultiplier;
		    return;
	    }


	    float MoreConsumptionMultiplier = 1;
	    float ThrustMultiplier = 0;

	    var Mix = pipeData.mixAndVolume;

	    var gasMix = pipeData.mixAndVolume.GetGasMix();
	    var ReagentMix = pipeData.mixAndVolume.GetReagentMix();
	    if ((gasMix.Pressure > 0 || ReagentMix.Total > 0) == false)
	    {
		    ThrustPower = 0;
		    return;
	    }

	    var Total = Mix.Total.x + Mix.Total.y;
	    foreach (var Reaction in ThrusterFuelReactions.Instance.ReactionMixes)
	    {
		    var PossibleMultiplier = GetReactionAmount(Mix, Reaction);

		    if (PossibleMultiplier == 0) continue;

		    var TotalChemicals = 0f;

		    var TotalFraction = 0f;
		    foreach (var gasOrReagent in Reaction.ChemicalMakeUp)
		    {
			    var Chemicals = Mix[gasOrReagent] * PossibleMultiplier;
			    TotalChemicals += Chemicals;
		    }

		    TotalFraction = TotalChemicals / Total;


		    if (TotalFraction > 0)
		    {
			    MoreConsumptionMultiplier += Reaction.ConsumptionMultiplierEffect;
			    ThrustMultiplier += Reaction.ThrustMultiplierEffect;
		    }
	    }

	    // var Ratio = ((Plasma / Oxygen) / (7f / 3f));
	    ThrustPower = TargetMolesUsed * ThrustMultiplier * ThrusterMultiplier;
	    var UsedMoles = TargetMolesUsed* MoreConsumptionMultiplier * AThrusterUseMultiplier;
	    Mix.Remove(new Vector2(UsedMoles, UsedMoles));
    }
}
