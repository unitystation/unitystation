using System;
using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using HealthV2;
using UnityEngine;

public class Eye : BodyPartFunctionality, IItemInOutMovedPlayer
{
	public Pickupable Pickupable;

	public MultiInterestBool HasXray = new MultiInterestBool();
	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

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
			//ShowForPlayer.PlayerScript.PlayerOnlySyncValues.XRay.RecordPosition(this, true);
		}

		if (HideForPlayer != null)
		{
			//HideForPlayer.PlayerScript.PlayerOnlySyncValues.XRay.RecordPosition(this, false);
		}


		//Logger.LogError("HideForPlayer > " + HideForPlayer?.name + "ShowForPlayer > " + ShowForPlayer?.name);
	}

	public void Awake()
	{

		Pickupable = this.GetComponent<Pickupable>();
	}

	public void Start()
	{

		HasXray.OnBoolChange.AddListener(UpdateXRay);
	}

	private void UpdateXRay(bool State)
	{

	}
}
