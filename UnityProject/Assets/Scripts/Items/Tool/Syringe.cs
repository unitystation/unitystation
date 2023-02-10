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

	private static readonly StandardProgressActionConfig injectProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);

	[SerializeField] private float injectTime = 0.55f;
	[SerializeField] private float armourMultiplier = 2.75f;

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
		if (LHB == null) return;
		TryInject(interaction.PerformerPlayerScript.RegisterPlayer, LHB);
	}

	private void TryInject(RegisterPlayer performer, LivingHealthMasterBase healthTarget)
	{
		var time = injectTime;
		foreach (var slot in healthTarget.playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.outerwear))
		{
			if (slot.IsEmpty) continue;
			time *= armourMultiplier;
		}
		StandardProgressAction.Create(injectProgressBar,
				() => InjectBehavior(healthTarget, performer))
			.ServerStartProgress(performer, time, performer.PlayerScript.gameObject);
	}

	private void InjectBehavior(LivingHealthMasterBase LHB, RegisterPlayer performer)
	{
		if (LocalContainer.ReagentMixTotal > 0)
		{
			if (LHB.CirculatorySystem != null)
				LHB.CirculatorySystem.BloodPool.Add(LocalContainer.TakeReagents(LocalContainer.ReagentMixTotal));
			LocalContainer.ReagentsChanged();
			Chat.AddCombatMsgToChat(performer.gameObject, $"You Inject The {this.name} into {LHB.gameObject.ExpensiveName()}",
				$"{performer.PlayerScript.visibleName} injects a {this.name} into {LHB.gameObject.ExpensiveName()}");
			if(SicknessesInSyringe.Count > 0) LHB.AddSickness(SicknessesInSyringe.PickRandom().Sickness);
			if (ChangesSprite) SpriteHandler.ChangeSprite(SpiteEmptyIndex);

		}
		else
		{
			if (LHB.CirculatorySystem != null)
				LocalContainer.Add(LHB.CirculatorySystem.BloodPool.Take(LocalContainer.MaxCapacity));
			LocalContainer.ReagentsChanged();
			if (ChangesSprite) SpriteHandler.ChangeSprite(SpiteFullIndex);
			Chat.AddCombatMsgToChat(performer.gameObject, $"You pull the blood from {LHB.gameObject.ExpensiveName()}",
				$"{performer.PlayerScript.visibleName} pulls the blood from {LHB.gameObject.ExpensiveName()}");
			if(LHB.mobSickness.sicknessAfflictions.Count > 0) SicknessesInSyringe.AddRange(LHB.mobSickness.sicknessAfflictions);
		}
	}
}