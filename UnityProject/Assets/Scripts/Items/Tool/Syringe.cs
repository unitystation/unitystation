using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Health.Sickness;
using HealthV2;
using Mirror;
using UnityEngine;

public class Syringe : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public ReagentContainer LocalContainer;

	public SpriteHandler SpriteHandler;


	public List<SicknessAffliction> SicknessesInSyringe = new List<SicknessAffliction>();

	public bool singleUse = false;

	private bool used = false;

	public bool ChangesSprite = true;

	public int SpiteFullIndex = 0;
	public int SpiteEmptyIndex = 1;

	public void Awake()
	{
		if (LocalContainer == null)
		{
			LocalContainer = this.GetComponent<ReagentContainer>();
		}

		if (SpriteHandler == null)
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
		}
	}

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
		if (singleUse)
		{
			if (used)
			{
				return;
			}
			used = true;
		}



		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		if (LHB != null)
		{
			if (LocalContainer.ReagentMixTotal > 0)
			{
				LHB.CirculatorySystem.BloodPool.Add(LocalContainer.TakeReagents(LocalContainer.ReagentMixTotal));
				LocalContainer.ReagentsChanged();
				Chat.AddActionMsgToChat(interaction.Performer, $"You Inject The {this.name} into {LHB.gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} injects a {this.name} into {LHB.gameObject.ExpensiveName()}");
				if(SicknessesInSyringe.Count > 0) LHB.AddSickness(SicknessesInSyringe.PickRandom().Sickness);
				if (ChangesSprite) SpriteHandler.ChangeSprite(SpiteEmptyIndex);

			}
			else
			{
				LocalContainer.Add(LHB.CirculatorySystem.BloodPool.Take(LocalContainer.MaxCapacity));
				LocalContainer.ReagentsChanged();
				if (ChangesSprite) SpriteHandler.ChangeSprite(SpiteFullIndex);
				Chat.AddActionMsgToChat(interaction.Performer, $"You pull the blood from {LHB.gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} pulls the blood from {LHB.gameObject.ExpensiveName()}");
				if(LHB.mobSickness.sicknessAfflictions.Count > 0) SicknessesInSyringe.AddRange(LHB.mobSickness.sicknessAfflictions);
			}
		}
	}
}