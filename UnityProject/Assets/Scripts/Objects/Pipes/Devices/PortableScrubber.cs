using System;
using System.Collections;
using System.Collections.Generic;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

public class PortableScrubber : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public Canister Canister;

	public bool CurrentState;

	public SpriteHandler Sprite;

	public UniversalObjectPhysics UniversalObjectPhysics;


	public float ScrubberEfficiency = 0.5f;


	public GasSO TargetGas = null;

	public List<Vector3Int> RelativePositionsToScrub = new List<Vector3Int>()
	{
		new Vector3Int(0, 0),
		//new Vector3Int(1, 0),
		//new Vector3Int(-1, 0),
		//new Vector3Int(0, 1),
		//new Vector3Int(0, -1),
		// new Vector3Int(-1, -1),
		// new Vector3Int(1, 1),
		// new Vector3Int(1, -1),
		// new Vector3Int(-1, 1),
		// new Vector3Int(2, 0),
		// new Vector3Int(-2, 0),
		// new Vector3Int(0, 2),
		// new Vector3Int(0, -2),
	};

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		return false;
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		// //Inserts reagent container
		// if (itemSlot.IsOccupied)
		// {
		// 	Chat.AddExamineMsgFromServer(interaction.Performer, "The machine already has a beaker in it");
		// 	return;
		// }
		//
		// //put the reagant container inside me
		// Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		// UpdateGUI();
	}

	public void ConnectCanister(Canister canister)
	{
		UniversalObjectPhysics.BuckleTo(canister.gameObject.GetUniversalObjectPhysics());
	}

	public void DisconnectCanister()
	{
		UniversalObjectPhysics.BuckledToObject.OrNull()?.Unbuckle();
	}

	public void Awake()
	{
		UniversalObjectPhysics = this.GetComponentCustom<UniversalObjectPhysics>();


		Canister.ServerOnConnectionStatusChange.AddListener(SetValve);
	}

	public void SetValve(bool State)
	{
		Canister.SetValve(State);
	}

	[NaughtyAttributes.Button]
	public void TurnOn()
	{
		Toggle(true);
	}

	public void Toggle(bool state)
	{
		CurrentState = state;
		UpdateState();
	}

	public void UpdateState()
	{
		if (CurrentState)
		{
			AtmosManager.Instance.AddUpdate(UpdateMe);
			Sprite.ChangeSprite(1);
		}
		else
		{
			AtmosManager.Instance.RemoveUpdate(UpdateMe);
			Sprite.ChangeSprite(0);
		}
	}

	public void OnDisable()
	{
		AtmosManager.Instance.RemoveUpdate(UpdateMe);
	}


	public void UpdateMe()
	{

		if (TargetGas == null)
		{
			Toggle(false);
			return;
		}

		var canister = Canister;
		if (canister == null)
		{
			Toggle(false);
			return;
		}

		var gasMix = canister.GasContainer.GasMix;
		if (gasMix.Pressure > 15000)
		{
			Toggle(false);
			return;
		}

		var energyTotal = 0f;
		var totalMoles = 0f;
		var localPosition = UniversalObjectPhysics.LocalTargetPosition.RoundToInt();

		foreach (var offsetPosition in RelativePositionsToScrub)
		{
			var tile = UniversalObjectPhysics.registerTile.Matrix.MetaDataLayer.Get(localPosition + offsetPosition);
			var moles = tile.GasMix.GetMoles(TargetGas) * ScrubberEfficiency;
			moles = moles.Clamp(0, 10); //max 25 Presumes it's only one
			if (moles == 0)
			{
				continue;
			}
			energyTotal += tile.GasMix.TakeGasReturnEnergy(TargetGas, moles);
			totalMoles += moles;
		}

		gasMix.AddGas(TargetGas, totalMoles, energyTotal);
	}
}
