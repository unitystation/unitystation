using System;
using Chemistry.Components;
using HealthV2;
using UnityEngine;

public class IVDrip : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>
{
	public ItemStorage ItemStorage;

	public SpriteHandler StemsSprite;
	public SpriteHandler BagSprite;
	public SpriteHandler BagLevelSprite;

	public StemState currentStemState = StemState.Idle;

	public ItemTrait BloodBag;

	public BagState currentBagState
	{
		get
		{
			if (bagSlot.Item == null)
			{
				return BagState.None;
			}

			if (Health == null)
			{
				return BagState.Present;
			}

			return BagState.Connected;
		}
	}

	private ItemSlot bagSlot;

	private LivingHealthMasterBase Health;

	public enum StemState
	{
		Idle = 0,
		DrainingBagSlow,
		DrainingBagFast,
		FillingBag
	}

	public enum BagState
	{
		None = 0,
		Present,
		Connected,
	}

	public void Awake()
	{
		bagSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
	}


	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (bagSlot.Item == null)
		{
			if (interaction.HandObject == null)
			{
				return;
			}

			if (Validations.HasItemTrait(interaction.HandObject, BloodBag) == false) return;

			Inventory.ServerTransfer(interaction.HandSlot, bagSlot);
			bagSlot.Item.GetComponent<ReagentContainer>().OnReagentMixChanged.AddListener(UpdateBagLevel);
			UpdateBagSprite();
			UpdateBagLevel();
			return;
		}
		else
		{
			if (interaction.HandObject == null && interaction.IsAltClick == false)
			{
				bagSlot.Item.GetComponent<ReagentContainer>().OnReagentMixChanged.RemoveListener(UpdateBagLevel);
				Inventory.ServerTransfer(bagSlot, interaction.HandSlot);

				currentStemState = StemState.Idle;
				UpdateBagSprite();
				UpdateBagLevel();
				return;
			}
		}

		if (currentStemState == StemState.FillingBag)
		{
			currentStemState = StemState.Idle;
		}
		else
		{
			currentStemState = (StemState) ((int) currentStemState + 1);
		}


		StemsSprite.SetCatalogueIndexSprite((int) currentStemState);
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		var Health = interaction.DroppedObject.GetComponent<LivingHealthMasterBase>();
		if (Health == null) return false;

		return true;
	}

	public void ServerPerformInteraction(MouseDrop drop)
	{
		var NewHealth = drop.DroppedObject.GetComponent<LivingHealthMasterBase>();
		if (NewHealth?.reagentPoolSystem == null)
		{
			return;
		}

		if (NewHealth != Health)
		{
			Health = NewHealth;
			UpdateManager.Add(UpdateMe, 1);
		}
		else
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			Health = null;
		}

		UpdateBagSprite();
	}

	public void UpdateBagLevel()
	{
		var Container = bagSlot?.Item?.GetComponent<ReagentContainer>();

		if (Container == null)
		{
			BagLevelSprite.SetCatalogueIndexSprite(0);
			return;
		}

		var Colour = Container.GetMixColor();
		BagLevelSprite.SetColor(Colour);
		if (Container.IsEmpty)
		{
			BagLevelSprite.SetCatalogueIndexSprite(0);
			return;
		}
		else if (Container.IsFull)
		{
			BagLevelSprite.SetCatalogueIndexSprite(7);
			return;
		}
		else
		{
			var Fraction = Container.ReagentMixTotal / Container.MaxCapacity;
			if (Fraction > 0.8)
			{
				BagLevelSprite.SetCatalogueIndexSprite(6);
				return;
			}

			if (Fraction > 0.75)
			{
				BagLevelSprite.SetCatalogueIndexSprite(5);
				return;
			}

			if (Fraction > 0.50)
			{
				BagLevelSprite.SetCatalogueIndexSprite(4);
				return;
			}

			if (Fraction > 0.25)
			{
				BagLevelSprite.SetCatalogueIndexSprite(3);
				return;
			}

			if (Fraction > 0.10)
			{
				BagLevelSprite.SetCatalogueIndexSprite(2);
				return;
			}

			BagLevelSprite.SetCatalogueIndexSprite(1);
			return;
		}
	}

	public void UpdateBagSprite()
	{
		BagSprite.SetCatalogueIndexSprite((int) currentBagState);
	}

	public void UpdateMe()
	{
		if (Health == null)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			return;
		}

		if (Health.reagentPoolSystem == null)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			return;
		}

		if (bagSlot.Item == null)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			return;
		}

		if ((Health.transform.position - this.transform.position).magnitude > 1.75f)
		{
			Health = null;
			UpdateBagSprite();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			return;
		}

		var ReagentContainer = bagSlot.Item.GetComponent<ReagentContainer>();


		switch (currentStemState)
		{
			case StemState.Idle:
				return;
			case StemState.DrainingBagSlow:
			case StemState.DrainingBagFast:
				//0.5u
				var TakeAmount = 0.5f;
				if (currentStemState == StemState.DrainingBagFast)
				{
					//5u
					TakeAmount = 5f;
				}

				Health.reagentPoolSystem.BloodPool.Add(ReagentContainer.TakeReagents(TakeAmount));
				if (ReagentContainer.IsEmpty)
				{
					currentStemState = StemState.Idle;
					StemsSprite.SetCatalogueIndexSprite((int) currentStemState);
				}

				break;
			case StemState.FillingBag:
				//5u
				ReagentContainer.Add(Health.reagentPoolSystem.BloodPool.Take(5f));

				if (ReagentContainer.IsFull)
				{
					currentStemState = StemState.Idle;
					StemsSprite.SetCatalogueIndexSprite((int) currentStemState);
				}

				break;
		}
	}
}