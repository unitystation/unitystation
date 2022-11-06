using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using HealthV2;
using Player;
using UnityEngine;

public class Eye : BodyPartFunctionality, IItemInOutMovedPlayer
{

	public bool VisionBlinded = false;

	public Pickupable Pickupable;

	public MultiInterestBool HasXray = new MultiInterestBool();
	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public ColourBlindMode ColourBlindMode;

	public int BaseBlurryVision = 0;



	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		//Valid if with an organ storage?
		//yeah
		if (Pickupable.ItemSlot == null) return false;

		if (player.PlayerScript.playerHealth.BodyPartStorage != Pickupable.ItemSlot.ItemStorage.GetRootStorage()) return false;

		//Am I also in the organ storage? E.G Part of the body
		if (RelatedPart.HealthMaster == null) return false;


		//Logger.LogError("IsValidSetup");
		return true;
	}

	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		if (ShowForPlayer != null)
		{
			var Synchronis = ShowForPlayer.PlayerScript.PlayerOnlySyncValues;
			Synchronis.SyncBlindness(false, VisionBlinded);
			UpdateXRay(true);
			UpdateColourblindValues();
			UpdateBlurryEye();
		}

		if (HideForPlayer != null)
		{
			var Synchronis = ShowForPlayer.PlayerScript.PlayerOnlySyncValues;
			Synchronis.SyncBlindness(false, true);
			Synchronis.XRay.RecordPosition(this, HasXray.State);
			Synchronis.SyncColourBlindMode(ColourBlindMode.None, ColourBlindMode.None);
			Synchronis.SyncBadEyesight(0,0);
		}

	}

	public override void AddedToBody(LivingHealthMasterBase AddedToBody)
	{
		var Synchronis = AddedToBody.GetComponent<PlayerOnlySyncValues>();

	}

	public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		var Synchronis = livingHealth.GetComponent<PlayerOnlySyncValues>();

	}

	public void Awake()
	{

		Pickupable = this.GetComponent<Pickupable>();
		RelatedPart.ModifierChange += UpdateBlurryEye;
	}

	public void Start()
	{
		HasXray.OnBoolChange.AddListener(UpdateXRay);
	}

	private void UpdateXRay(bool State)
	{
		if (CurrentlyOn != null)
		{
			CurrentlyOn.PlayerScript.PlayerOnlySyncValues.XRay.RecordPosition(this, HasXray.State);
		}
	}

	public void UpdateColourblindValues()
	{
		if (CurrentlyOn != null)
		{
			CurrentlyOn.PlayerScript.PlayerOnlySyncValues.SyncColourBlindMode(ColourBlindMode.None, ColourBlindMode);
		}
	}

	public void UpdateBlurryEye()
	{
		if (CurrentlyOn != null)
		{
			if (RelatedPart.TotalModified < 0.75f)
			{
				var Calculated =Mathf.RoundToInt(30 * (1 - (RelatedPart.TotalModified / 0.75f)));
				Calculated = Calculated + BaseBlurryVision;
				CurrentlyOn.PlayerScript.PlayerOnlySyncValues.SyncBadEyesight(0,(uint)Calculated);
			}
		}

	}
}
