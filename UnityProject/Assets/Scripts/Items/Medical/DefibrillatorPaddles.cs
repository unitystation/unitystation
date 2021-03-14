﻿using System.Collections;
using System.Collections.Generic;
using System.Data;
using HealthV2;
using Items;
using UnityEngine;

public class DefibrillatorPaddles : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public ItemTrait DefibrillatorTrait;

	public StandardProgressActionConfig StandardProgressActionConfig;

	public float Time;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.HandApply(interaction,side) == false) return false;
		if (interaction.HandObject != this.gameObject) return false;
		var Healthv2 = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (Healthv2 == null) return false;
		var equipment = interaction.Performer.GetComponent<Equipment>();
		if (equipment == null) return false;
		var ObjectInSlot = equipment.GetClothingItem(NamedSlot.back).GameObjectReference;
		if (ObjectInSlot == null) return false;
		if (Validations.HasItemTrait(ObjectInSlot, DefibrillatorTrait) == false) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		void Perform()
		{
			var LHMB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			foreach (var BodyPart in LHMB.ImplantList)
			{
				var heart = BodyPart as Heart;
				if (heart != null)
				{
					heart.HeartAttack = false;
					heart.CanTriggerHeartAttack = false;
					heart.CurrentPulse = 0;
				}
			}
		}
		var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true), Perform);
		bar.ServerStartProgress(interaction.Performer.RegisterTile(), Time, interaction.Performer);
	}
}
