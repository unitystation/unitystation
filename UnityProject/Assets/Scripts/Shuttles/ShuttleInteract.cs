using PlayGroups.Input;
using UI;
using UnityEngine;

public class ShuttleInteract : InputTrigger 
{
    public string interactionMessage;
    public MatrixMove ShuttleMatrixMove;
    private GameObject player;
    
    // Method that brings up shuttle controls.
    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        player = originator;
        ControlTabs.ShowShuttleTab(ShuttleMatrixMove);
    }

    /// <summary>
    /// Checks if the player has moved away from the shuttle. If the player has moved away it hides the tab.
    /// </summary>
    public void Update()
    {
        if ( player != null && Vector3.Distance(player.transform.position, transform.position) > 1.5f )
        {
            Debug.Log("Player walked away from console.");
            ControlTabs.HideShuttleTab();
            player = null;
        }
    }
}
