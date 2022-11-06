using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using HealthV2;
using Mirror;
using Player;
using UnityEngine;

public class Eye : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect
{
	//TODO
	//Probably should make it so the shader has Multi-interest bool /  Think of a system to We never have this issue with Things overlap with what they control
	//X-ray, colourblindness, Blindness have issues currentlyZ

	public Pickupable Pickupable;

	public int BaseBlurryVision = 0;

	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		//Valid if with an organ storage?
		//yeah
		if (Pickupable.ItemSlot == null) return false;

		if (player.PlayerScript.playerHealth.BodyPartStorage !=
		    Pickupable.ItemSlot.ItemStorage.GetRootStorage()) return false;

		//Am I also in the organ storage? E.G Part of the body
		if (RelatedPart.HealthMaster == null) return false;


		//Logger.LogError("IsValidSetup");
		return true;
	}

	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		if (ShowForPlayer != null)
		{
			OnBodyID = ShowForPlayer.netId;
		}
		else
		{
			OnBodyID = NetId.Empty;
		}
	}

	public void Awake()
	{
		Pickupable = this.GetComponent<Pickupable>();
		RelatedPart.ModifierChange += UpdateBlurryEye;
	}


	public void UpdateBlurryEye()
	{
		int Calculated = 0;
		if (RelatedPart.TotalModified < 0.95f)
		{
			Calculated = Mathf.RoundToInt(30 * (1 - (RelatedPart.TotalModified / 0.95f)));
		}

		Calculated = Calculated + BaseBlurryVision;
		SyncBadEyesight(0, Calculated);
	}

	[NaughtyAttributes.Button()]
	public void MakeBlind()
	{
		SyncBlindness(false, true);
	}


	[NaughtyAttributes.Button()]
	public void GivesSite()
	{
		SyncBlindness(false, false);
	}


	#region Synchronise

	private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

	[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

	public uint OnPlayerID => OnBodyID;

	[SyncVar(hook = nameof(SyncBlindness))]
	public bool isBlind = false; //TODO change to multi-interest bool, Is good enough for now, For multiple eyes
	public bool DefaultisBlind = false;

	[SyncVar(hook = nameof(SyncBadEyesight))]
	public int BadEyesight = 0;
	private int DefaultBadEyesight = 0;

	[SyncVar(hook = nameof(SyncColourBlindMode))]
	public ColourBlindMode CurrentColourblindness = ColourBlindMode.None;
	public ColourBlindMode DefaultColourblindness = ColourBlindMode.None;

	[SyncVar(hook = nameof(SyncXrayState))]
	public bool HasXray = false;
	public bool DefaultHasXray = false;



	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		OnBodyID = CurrentlyOn;
		Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
	}

	public void ApplyDefaultOrCurrentValues(bool Default)
	{
		ApplyChangesBlindness(Default ? DefaultisBlind : isBlind);
		ApplyChangesBlurryVision(Default ? DefaultBadEyesight : BadEyesight);
		ApplyChangesColourBlindMode(Default ? DefaultColourblindness : CurrentColourblindness);
		ApplyChangesXrayState(Default ? DefaultHasXray : HasXray);
	}


	public void SyncBlindness(bool oldValue, bool newState)
	{
		isBlind = newState;
		if (Preimplemented.IsOnLocalPlayer)
		{
			ApplyChangesBlindness(isBlind);
		}
	}

	public void ApplyChangesBlindness(bool SetValue)
	{
		if (SetValue)
		{
			Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.fovDistance = 0.65f;
		}
		else
		{
			Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.fovDistance = 15;
		}
	}


	public void SyncBadEyesight(int oldValue, int newState)
	{
		BadEyesight = newState;
		if (Preimplemented.IsOnLocalPlayer)
		{
			ApplyChangesBlurryVision((int) BadEyesight);
		}
	}

	public void ApplyChangesBlurryVision(int BlurryStrength)
	{
		Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().blurryVisionEffect
			.SetBlurStrength((int) BlurryStrength);
	}


	public void SyncColourBlindMode(ColourBlindMode NotSetValueServer, ColourBlindMode newState)
	{
		CurrentColourblindness = newState;
		if (Preimplemented.IsOnLocalPlayer)
		{
			ApplyChangesColourBlindMode(CurrentColourblindness);
		}
	}

	public void ApplyChangesColourBlindMode(ColourBlindMode newState)
	{
		Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().colourblindEmulationEffect
			.SetColourMode(newState);
	}




	private void SyncXrayState(bool old, bool newState)
	{
		HasXray = newState;
		if (Preimplemented.IsOnLocalPlayer)
		{
			ApplyChangesXrayState(HasXray);
		}
	}

	public void ApplyChangesXrayState(bool newState)
	{
		if (newState)
		{
			Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.renderSettings
				.fovOcclusionSpread = 3;
		}
		else
		{
			Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().LightingSystem.renderSettings
				.fovOcclusionSpread = 0;
		}
	}

	#endregion
}