using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Health.Sickness;
using HealthV2;
using Mirror;
using UnityEngine;

public class Syringe : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
{
	public ReagentContainer LocalContainer;

	public SpriteHandler SpriteHandler;

	public SpriteHandler PullOrDrawSpriteHandler;

	public SpriteHandler ContentsSpriteHandler;

	public List<SicknessAffliction> SicknessesInSyringe = new List<SicknessAffliction>();

	public bool singleUse = false;

	private bool used = false;

	public bool ChangesSprite = true;

	public int SpiteFullIndex = 0;
	public int SpiteEmptyIndex = 1;

	public float TransferAmount = 5;

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

		LocalContainer?.OnReagentMixChanged?.AddListener(ColourContentsChange);

	}

	public void ColourContentsChange()
	{
		if (ContentsSpriteHandler == null) return;
		ContentsSpriteHandler.SetColor(LocalContainer.CurrentReagentMix.MixColor);
		var Fraction = LocalContainer.ReagentMixTotal / LocalContainer.MaxCapacity;


		if (Fraction >= 0.999f)
		{
			ContentsSpriteHandler.SetCatalogueIndexSprite(4);
		}
		else if (Fraction > 0.65f)
		{
			ContentsSpriteHandler.SetCatalogueIndexSprite(3);
		}
		else if (Fraction > 0.32f)
		{
			ContentsSpriteHandler.SetCatalogueIndexSprite(2);
		}
		else if (Fraction > 0.10f)
		{
			ContentsSpriteHandler.SetCatalogueIndexSprite(2);
		}
		else
		{
			ContentsSpriteHandler.SetCatalogueIndexSprite(0);
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

		Chat.AddCombatMsgToChat(performer.gameObject,
			$"You try to stick the {this.name} into {healthTarget.gameObject.ExpensiveName()}",
			$"{performer.PlayerScript.visibleName} Tries to stick a {this.name} into {healthTarget.gameObject.ExpensiveName()}");

		StandardProgressAction.Create(injectProgressBar,
				() => InjectBehavior(healthTarget, performer))
			.ServerStartProgress(performer, time, performer.PlayerScript.gameObject);
	}

	public virtual void InjectBehavior(LivingHealthMasterBase LHB, RegisterPlayer performer)
	{
		used = true;
		if (LocalContainer.SyringePulling == false)
		{
			if (LHB.reagentPoolSystem != null)
				LHB.reagentPoolSystem.BloodPool.Add(LocalContainer.TakeReagents(TransferAmount));
			LocalContainer.ReagentsChanged();
			Chat.AddCombatMsgToChat(performer.gameObject,
				$"You Inject The {this.name} into {LHB.gameObject.ExpensiveName()}",
				$"{performer.PlayerScript.visibleName} injects a {this.name} into {LHB.gameObject.ExpensiveName()}");
			if (SicknessesInSyringe.Count > 0) LHB.AddSickness(SicknessesInSyringe.PickRandom().Sickness);
			if (ChangesSprite) SpriteHandler.SetCatalogueIndexSprite(SpiteEmptyIndex);

			if (LocalContainer.ReagentMixTotal == 0)
			{
				SetSyringeState(true);
			}
		}
		else
		{
			if (LHB.reagentPoolSystem != null)
				LocalContainer.Add(LHB.reagentPoolSystem.BloodPool.Take(LocalContainer.MaxCapacity));
			LocalContainer.ReagentsChanged();
			if (ChangesSprite) SpriteHandler.SetCatalogueIndexSprite(SpiteFullIndex);
			Chat.AddCombatMsgToChat(performer.gameObject, $"You pull the blood from {LHB.gameObject.ExpensiveName()}",
				$"{performer.PlayerScript.visibleName} pulls the blood from {LHB.gameObject.ExpensiveName()}");
			if (LHB.mobSickness.sicknessAfflictions.Count > 0)
				SicknessesInSyringe.AddRange(LHB.mobSickness.sicknessAfflictions);


			if (LocalContainer.ReagentMixTotal == LocalContainer.MaxCapacity)
			{
				SetSyringeState( false);
			}
		}
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		SetSyringeState( !LocalContainer.SyringePulling);
		if (LocalContainer.IsFull && LocalContainer.SyringePulling)
		{
			SetSyringeState(false);
			Chat.AddExamineMsg(interaction.Performer,
				$"The {gameObject.ExpensiveName()} Is full, you can't pull any more.");
			return;
		}

		if (LocalContainer.IsEmpty && LocalContainer.SyringePulling == false)
		{
			SetSyringeState(true);
			Chat.AddExamineMsg(interaction.Performer,
				$"The {gameObject.ExpensiveName()} Is empty, there's nothing to inject.");
			return;
		}

		var pullstreing = LocalContainer.SyringePulling ? "Pulling mode" : "Injecting mode";
		Chat.AddExamineMsg(interaction.Performer,
			$"You change {gameObject.ExpensiveName()} to {pullstreing}.");
	}

	public void SetSyringeState(bool Draw)
	{
		if (Draw)
		{
			LocalContainer.SyringePulling = true;
			if (PullOrDrawSpriteHandler != null)
			{
				PullOrDrawSpriteHandler.SetCatalogueIndexSprite(1);
			}
		}
		else
		{
			LocalContainer.SyringePulling = false;
			if (PullOrDrawSpriteHandler != null)
			{
				PullOrDrawSpriteHandler.SetCatalogueIndexSprite(0);
			}
		}

	}
}