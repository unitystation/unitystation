using UnityEngine;
using HealthV2;
using Mirror;

public class Implanter : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>, ICheckedInteractable<HandActivate>
{
	[SerializeField,Tooltip("The implant that this implanter starts with, leave as null if implanter is inted to be empty.")]
	private GameObject implantObject = null;

	[SerializeField]
	private int timeToImplant = 5;

	[SerializeField]
	private ItemTrait ImplantableTrait = null;

	private SpriteHandler spriteHandler;

	private ItemStorage itemStorage;

	private ItemSlot implantSlot;

	[SyncVar] private bool primed = false;


	private void Awake()
	{
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		itemStorage = GetComponent<ItemStorage>();
		implantSlot = itemStorage.GetIndexedItemSlot(0);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (implantObject != null)
		{
			Inventory.ServerSpawnPrefab(implantObject, implantSlot);
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		//can only be applied to LHB
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;

		if (interaction.Intent != Intent.Help || primed == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var lhb = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();

		bool hasFoundPart = false; //A player can have multiple surface body parts in the same 'target' such as multiple right arms. This bool is here so it only tries to implant into one bodypart.

		foreach (var bodyPart in lhb.SurfaceBodyParts)
		{
			if (bodyPart.BodyPartType == interaction.TargetBodyPart && hasFoundPart == false)
			{
				ItemSlot toSlot = bodyPart.OrganStorage.GetNextFreeIndexedSlot();

				if (toSlot != null)
				{
					hasFoundPart = true;

					ToolUtils.ServerUseToolWithActionMessages(interaction, timeToImplant,
					$"You begin injecting {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter...",
					$"{interaction.Performer.ExpensiveName()} begins injecting {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter...",
					$"You inject {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter.",
					$"{interaction.Performer.ExpensiveName()} injects {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter.",
					() =>
					{
						if (Inventory.ServerTransfer(implantSlot, toSlot, ReplacementStrategy.DespawnOther))
						{
							SetPrimed(false);
						}
						else
						{
							Chat.AddWarningMsgFromServer(interaction.Performer, "Unable to inject implant! The target body part might not have room for this implant!");
						}


					});
				}
			}
		}
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
		{
			if (interaction.UsedObject != null)
			{
				Inventory.ServerTransfer(interaction.FromSlot, implantSlot);
			}
			else
			{
				Inventory.ServerTransfer(implantSlot, interaction.FromSlot);
			}
		}
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if(DefaultWillInteract.Default(interaction, side) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (interaction.IsAltClick)
		{
			SetPrimed(false);
			itemStorage.ServerDropAll();
		}
		else
		{
			if (itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Chat.AddExamineMsg(interaction.Performer, "Cannot prime without implant");
				SetPrimed(false);
			}
			else
			{
				SetPrimed(!primed);
			}
		}
	}

	private void SetPrimed(bool isPrimed)
	{
		primed = isPrimed;
		var index = primed == true ? 1 : 0;

		spriteHandler.ChangeSprite(index);
	}


}
