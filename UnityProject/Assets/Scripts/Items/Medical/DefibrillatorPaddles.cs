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
		if (interaction.TargetObject.GetComponent<LivingHealthMasterBase>() == null)
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
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		void Perform()
		{
			var LHMB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (LHMB.brain == null || LHMB.brain?.RelatedPart?.Health < -100)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "It appears they're missing their brain or Their brain is too damaged");
			}

			foreach (var BodyPart in LHMB.BodyPartList)
			{
				foreach (var organ in BodyPart.OrganList)
				{
					if (organ is Heart heart)
					{
						heart.HeartAttack = false;
						heart.CanTriggerHeartAttack = false;
						heart.CurrentPulse = 0;
					}
				}
			}

			LHMB.CalculateOverallHealth();
		}
		var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true), Perform);
		bar.ServerStartProgress(interaction.Performer.RegisterTile(), Time, interaction.Performer);
	}
}
