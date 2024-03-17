using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Core.Sprite_Handler;
using Light2D;
using Messages.Server.SoundMessages;
using Mirror;
using Objects.Atmospherics;
using Systems.Atmospherics;
using Systems.Pipes;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

public class Thruster : MonoPipe
{
	public MatrixMove RelatedMove;
	private RegisterTile RegisterTile;
	public Rotatable Rotatable;

	public ThrusterDirectionClassification ThisThrusterDirectionClassification;

	public float MaxMolesUseda = 1;
	public float TargetMolesUsed = 0;

	public float ThrusterMultiplier = 100;

	public float AThrusterUseMultiplier = 0.072975f;

	[SyncVar]
	public float ThrustPower;

	public bool SelfPowered = false;

	private ParticleSystem particleFX;
	private LightSpriteHandler lightSprite;

	public float MaxEngineParticles = 70f;

	[SyncVar(hook = nameof(SynchronisePreviousUsageAffects))]
	public float PreviousUsage = float.NaN;

	public float SetUsage = float.NaN;

	[SerializeField]
	[Tooltip("The looped audio source to play while the griddle is running.")]
	private AddressableAudioSource RunningAudio = default;

	private string audioLoopGUID = string.Empty;

	public float InletPressure = 0;

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
		Up,
		Right,
		Left,
		Down
	}


	public void OnDestroy()
	{
		if (RelatedMove != null)
		{
			RelatedMove.NetworkedMatrixMove.RemoveThruster(this);
		}
	}


	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	// Start is called before the first frame update
    void Start()
    {
	    ReregisterThruster();
	    Rotatable = this.GetComponentCustom<Rotatable>();
	    RegisterTile = this.GetComponent<RegisterTile>();

	    lightSprite = GetComponentInChildren<LightSpriteHandler>();
	    RegisterTile.OnParentChangeComplete.AddListener(ReregisterThruster);
    }

    public void ReregisterThruster()
    {
	    var NewRelatedMove = this.GetComponentInParent<MatrixMove>();
	    if (RelatedMove != null)
	    {
		    RelatedMove.NetworkedMatrixMove.RemoveThruster(this);
	    }

	    if (NewRelatedMove != null)
	    {
		    RelatedMove = NewRelatedMove;
		    NewRelatedMove.NetworkedMatrixMove.AddThruster(this);
	    }
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


    public void UpdateMe()
    {
	    SetEffects(SetUsage);
    }

    public override void TickUpdate()
    {

	    if (TargetMolesUsed == 0)
	    {
		    AtmosphericsSetUsage(TargetMolesUsed);
		    return;
	    }
	    pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

	    if (pipeData.SelfSufficient)
	    {
		    ThrustPower = TargetMolesUsed  * ThrusterMultiplier;
		    AtmosphericsSetUsage(TargetMolesUsed);
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
		    AtmosphericsSetUsage(0);
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
	    AtmosphericsSetUsage(UsedMoles);
	    InletPressure = Mix.GetGasMix().Pressure;
	    Mix.Remove(new Vector2(UsedMoles, UsedMoles));

    }

    public void SetEffects(float CurrentUsage)
    {
	    SynchronisePreviousUsageAffects(PreviousUsage, CurrentUsage);
    }

    public void AtmosphericsSetUsage(float CurrentUsage)
    {
	    SetUsage = CurrentUsage;
    }

    public void SynchronisePreviousUsageAffects(float old, float newv)
    {
	    PreviousUsage = newv;
	    if (particleFX == null)
	    {
		    particleFX = GetComponentInChildren<ParticleSystem>();
	    }

	    var emissionFX = particleFX.emission;
	    if (old != newv)
	    {
		    var Fraction = TargetMolesUsed / MaxMolesUseda;

		    if (string.IsNullOrEmpty(audioLoopGUID) == false)
		    {
			    if (Fraction == 0)
			    {
				    SoundManager.ClientStop(audioLoopGUID, true);
				    audioLoopGUID = "";
			    }
			    else
			    {
				    SoundManager.ChangeAudioSourceParameters(audioLoopGUID,  new AudioSourceParameters(pitch:0.45f, volume: Fraction, loops: true));
			    }
		    }
		    else
		    {
			    if (Fraction != 0)
			    {
				    audioLoopGUID = Guid.NewGuid().ToString();
				    SoundManager.ClientPlayAtPositionAttached(RunningAudio, registerTile.WorldPosition, gameObject, audioLoopGUID,
					    audioSourceParameters: new AudioSourceParameters(pitch: 0.45f, volume: Fraction, loops: true));
			    }
		    }

		    PreviousUsage = newv;
		    var colour = lightSprite.CurrentColor;
		    colour.a =  Fraction;
		    lightSprite.SetColor(colour);

		    if (newv == 0)
		    {
			    emissionFX.enabled = false;
		    }
		    else
		    {
			    emissionFX.enabled = true;
			    emissionFX.rateOverTime = Mathf.Clamp(Fraction * MaxEngineParticles, 30f, 70f);
		    }


	    }
    }
}
