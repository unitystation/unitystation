﻿using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AddressableReferences;
using HealthV2;
using UnityEngine;

namespace Items.Medical
{
	public class DefibrillatorPaddles : MonoBehaviour, ICheckedInteractable<HandApply>, IInteractable<HandActivate>
{
	public ItemTrait DefibrillatorTrait;

	public StandardProgressActionConfig StandardProgressActionConfig;

	public float Time;

	[SerializeField] private AddressableAudioSource soundCharged;
	[SerializeField] private AddressableAudioSource soundReady;
	[SerializeField] private AddressableAudioSource soundSuccsuess;
	[SerializeField] private AddressableAudioSource soundFailed;
	[SerializeField] private AddressableAudioSource soundZap;

	private bool isReady;
	private bool onCooldown;
	private readonly int cooldownMs = 5000;

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
				_ = SoundManager.PlayNetworkedAtPosAsync(soundFailed, gameObject.AssumedWorldPosServer());
				Cooldown();
				return;
			}
			livingHealthMaster.RestartHeart();
			_ = SoundManager.PlayNetworkedAtPosAsync(soundZap, gameObject.AssumedWorldPosServer());
			if (livingHealthMaster.IsDead == false)
			{
				livingHealthMaster.playerScript.ReturnGhostToBody();
				_ = SoundManager.PlayNetworkedAtPosAsync(soundSuccsuess, gameObject.AssumedWorldPosServer());
				Cooldown();
				return;
			}
			_ = SoundManager.PlayNetworkedAtPosAsync(soundFailed, gameObject.AssumedWorldPosServer());
			Cooldown();
		}

		if (isReady == false || onCooldown == true)
		{
			Chat.AddExamineMsg(interaction.Performer, $"You need to charge the {gameObject.ExpensiveName()} first!");
			return;
		}
		var bar = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true), Perform);
		bar.ServerStartProgress(interaction.Performer.RegisterTile(), Time, interaction.Performer);
	}

	private async void Cooldown()
	{
		onCooldown = true;
		await Task.Delay(cooldownMs).ConfigureAwait(false);
		_ = SoundManager.PlayNetworkedAtPosAsync(soundCharged, gameObject.AssumedWorldPosServer());
		onCooldown = false;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (onCooldown)
		{
			Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is still charging!");
			return;
		}
		if (isReady == false)
		{
			Chat.AddExamineMsg(interaction.Performer, $"You prepare the {gameObject.ExpensiveName()}");
			isReady = true;
			_ = SoundManager.PlayNetworkedAtPosAsync(soundReady, gameObject.AssumedWorldPosServer());
			return;
		}
		Chat.AddExamineMsg(interaction.Performer, $"<color=green>The {gameObject.ExpensiveName()} is charged and ready to be used.</color>");
	}
}

}
