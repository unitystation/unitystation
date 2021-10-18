using System.Collections;
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
		if (DefaultWillInteract.Default(interaction,side) == false)
			return false;
		var livingHealthMaster = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (livingHealthMaster == null)
			return false;
		var equipment = interaction.Performer.GetComponent<Equipment>();
		var ObjectInSlot = equipment.GetClothingItem(NamedSlot.back).GameObjectReference;
		if (Validations.HasItemTrait(ObjectInSlot, DefibrillatorTrait) == false)
		{
			ObjectInSlot = equipment.GetClothingItem(NamedSlot.belt).GameObjectReference;
			if (Validations.HasItemTrait(ObjectInSlot, DefibrillatorTrait) == false)
			{
				return false;
			}
		}
		if (CanDefibrillate(livingHealthMaster, interaction.Performer) == false && side == NetworkSide.Server)
		{
			return false;
		}
		return true;
	}

	private bool CanDefibrillate(LivingHealthMasterBase livingHealthMaster, GameObject performer)
	{
		if (livingHealthMaster.brain == null || livingHealthMaster.brain.RelatedPart.Health < -100)
		{
			Chat.AddExamineMsgFromServer(performer, "It appears they're missing their brain or Their brain is too damaged");
			return false;
		}
		if (livingHealthMaster.IsDead == false)
		{
			Chat.AddExamineMsgFromServer(performer, "The target is alive!");
			return false;
		}
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		void Perform()
		{
			var livingHealthMaster = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (CanDefibrillate(livingHealthMaster, interaction.Performer) == false)
			{
				return;
			}
			livingHealthMaster.RestartHeart();
			if (livingHealthMaster.IsDead == false)
			{
				livingHealthMaster.playerScript.ReturnGhostToBody();
			}
		}
		var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true), Perform);
		bar.ServerStartProgress(interaction.Performer.RegisterTile(), Time, interaction.Performer);
	}
}
