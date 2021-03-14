﻿using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using HealthV2;
using Mirror;
using UnityEngine;

public class Syringe : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public ReagentContainer LocalContainer;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;

		return true;
	}

	/// <summary>
	/// Server handles hand interaction with tray
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB != null)
		{
			LHB.CirculatorySystem.ReadyBloodPool.Add(LocalContainer.TakeReagents(10f));
		}
	}
}
