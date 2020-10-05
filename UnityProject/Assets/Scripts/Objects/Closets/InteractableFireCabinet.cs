using UnityEngine;
using Mirror;

/// <summary>
/// Main logic for fire cabinet, allows storing an extinguisher.
/// </summary>
[RequireComponent(typeof(ItemStorage))]
public class InteractableFireCabinet : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public bool IsClosed;

	[SerializeField]
	private SpriteHandler spriteHandler = default;
	private ItemStorage storageObject;
	private ItemSlot slot;

	private enum FireCabinetState
	{
		Closed = 0,
		OpenEmpty = 1,
		OpenFull = 2,
		/// <summary> OpenMini represents Open state containing pocket extinguisher. Not implemented. </summary>
		OpenMini = 3
	};

	private void Awake()
	{
		EnsureInit();
		//TODO: Can probably refactor this component to rely more on ItemStorage and do less of its own logic.
	}

	private void EnsureInit()
	{
		if (storageObject != null) return;
		//we have an item storage with only 1 slot.
		storageObject = GetComponent<ItemStorage>();
		slot = storageObject.GetIndexedItemSlot(0);
	}

	public override void OnStartServer()
	{
		EnsureInit();
		IsClosed = true;
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only allow interactions targeting this
		if (interaction.TargetObject != gameObject) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//If alt is pressed, close the cabinet.
		if (interaction.IsAltClick)
		{
			if (!IsClosed)
			{
				Close();
			}
		}
		else // Take out or put in object into cabinet.
		{
			if (IsClosed)
			{
				if(slot.Item != null && interaction.HandObject == null)
				{
					ServerRemoveExtinguisher(interaction.HandSlot);
				}
				Open();
			}
			else
			{
				if (slot.Item != null)
				{
					if (interaction.HandObject == null)
					{
						ServerRemoveExtinguisher(interaction.HandSlot);
					}
					else
					{
						Close();
					}
				}
				else
				{
					if (interaction.HandObject && interaction.HandObject.GetComponent<FireExtinguisher>())
					{
						ServerAddExtinguisher(interaction);
					}
					else
					{
						Close();
					}
				}
			}
		}
	}

	private void ServerRemoveExtinguisher(ItemSlot toSlot)
	{
		if (Inventory.ServerTransfer(slot, toSlot))
		{
			ServerSetState(FireCabinetState.OpenEmpty);
		}
	}

	private void ServerAddExtinguisher(HandApply interaction)
	{
		if (Inventory.ServerTransfer(interaction.HandSlot, slot))
		{
			ServerSetState(FireCabinetState.OpenFull);
		}
	}

	private void Open()
	{
		IsClosed = false;
		SoundManager.PlayAtPosition("OpenClose", transform.position, gameObject);
		if (slot.Item != null)
		{
			ServerSetState(FireCabinetState.OpenFull);
		}
		else
		{
			ServerSetState(FireCabinetState.OpenEmpty);
		}
	}

	private void Close()
	{
		IsClosed = true;
		SoundManager.PlayAtPosition("OpenClose", transform.position, gameObject);
		ServerSetState(FireCabinetState.Closed);
	}

	private void ServerSetState(FireCabinetState newState)
	{
		spriteHandler.ChangeSprite((int) newState);
	}
}
