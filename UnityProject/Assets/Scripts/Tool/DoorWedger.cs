using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorWedger : MonoBehaviour, IInteractable<HandApply>, ICheckedInteractable<HandApply>
{
    [SerializeField] [Tooltip("The time in seconds it takes to wedge open a door with this tool.")]
    private float openingDelay = 10f;

    public bool WillInteract(HandApply interaction, NetworkSide side)
    {
        if (!DefaultWillInteract.Default(interaction, side)) return false;

        if (Validations.IsTarget(gameObject, interaction)) return false;
        return true;
    }
    
    public void ServerPerformInteraction(HandApply interaction)
    {
        var targetObject = interaction.TargetObject.GetComponent<DoorController>();

        if (targetObject == null)
        {
            return;
        }

        if (targetObject.IsWelded)
        {
            Chat.AddExamineMsgFromServer(interaction.Performer, $"You can't force this door open with your {gameObject.ExpensiveName()}, it's welded shut!");
        }

        if (targetObject.IsClosed)
        {
            ToolUtils.ServerUseToolWithActionMessages(interaction, openingDelay,
            "You start wedging open the door...",
            $"{interaction.Performer.ExpensiveName()} starts wedging open the door...",
            $"You force the door open with your {gameObject.ExpensiveName()}!",
            $"{interaction.Performer.ExpensiveName()} forces the door open!",
            () =>
            {
                targetObject.ServerOpen();
            });
        }
    }
}
