using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MouseDraggable))]
public class ChairDraggable : MonoBehaviour, ICheckedInteractable<MouseDrop>
{
	[SerializeField] private GameObject prefabVariant = null;
    public bool WillInteract(MouseDrop interaction, NetworkSide side)
    {
	    if (!DefaultWillInteract.Default(interaction, side))
	    {
		    return false;
	    }

	    if (MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
		    .Any(pm => pm.IsBuckled))
	    {
		    return false;
	    }
	    var cnt = GetComponent<CustomNetTransform>();
	    var ps = interaction.Performer.GetComponent<PlayerScript>();
	    var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

	    return pna && interaction.Performer == interaction.TargetObject
	               && interaction.DroppedObject == gameObject
	               && pna.GetActiveHandItem() == null
	               && ps.IsInReach(cnt.RegisterTile, side == NetworkSide.Server);
    }

    public void ServerPerformInteraction(MouseDrop interaction)
    {
	    var folded = Spawn.ServerPrefab(prefabVariant).GameObject;
	    Inventory.ServerAdd(folded,
		    interaction.Performer.GetComponent<ItemStorage>().GetActiveHandSlot());
	    // Remove from world
	    Despawn.ServerSingle(gameObject);
    }
}
