using UnityEngine;
using HealthV2;
using Items;

public class Implanter : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>, ICheckedInteractable<HandActivate>,IServerSpawn
{
	[SerializeField]
	private GameObject implantObject = null;

	[SerializeField]
	private int timeToImplant = 5;

	[SerializeField]
	private ItemTrait ImplantableTrait = null;

	[SerializeField]
	private SpriteHandler spriteHandler;

	private ItemStorage itemStorage;
	private bool primed = false;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (implantObject != null)
		{
			Inventory.ServerSpawnPrefab(implantObject, itemStorage.GetIndexedItemSlot(0));
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;

		if (interaction.Intent != Intent.Help) return false;

		if(itemStorage.GetIndexedItemSlot(0).IsEmpty)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "No implant present in implanter!");
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();

		foreach (var bodyPart in LHB.SurfaceBodyParts)
		{
			bool selected = false;

			if (bodyPart.BodyPartType == interaction.TargetBodyPart)
			{
				ItemSlot toSlot = bodyPart.OrganStorage.GetNextFreeIndexedSlot();
				ItemSlot fromSlot = itemStorage.GetIndexedItemSlot(0);

				if (toSlot != null && selected == false)
				{
					selected = true;

					ToolUtils.ServerUseToolWithActionMessages(interaction, timeToImplant,
					$"You begin injecting {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter...",
					$"{interaction.Performer.ExpensiveName()} begins injecting {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter...",
					$"You inject {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter.",
					$"{interaction.Performer.ExpensiveName()} injects {interaction.TargetObject.ExpensiveName()}'s {bodyPart.gameObject.ExpensiveName()} with the implanter.",
					() =>
					{
						Inventory.ServerTransfer(fromSlot, toSlot);
						TogglePrimed();
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
				Inventory.ServerTransfer(interaction.FromSlot, itemStorage.GetIndexedItemSlot(0));
			}
			else
			{
				Inventory.ServerTransfer(itemStorage.GetIndexedItemSlot(0), interaction.FromSlot);
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
			itemStorage.ServerDropAll();
		}
		else
		{
			if (itemStorage.GetIndexedItemSlot(0).IsEmpty) Chat.AddExamineMsg(interaction.Performer, "Cannot prime without implant");
			else TogglePrimed();
		}
	}

	private void TogglePrimed()
	{
		primed = !primed;
		var index = primed == true ? 1 : 0;

		spriteHandler.ChangeSprite(index);
	}


}
