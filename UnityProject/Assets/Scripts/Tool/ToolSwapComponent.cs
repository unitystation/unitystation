using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolSwapComponent : MonoBehaviour, IInteractable<HandActivate>
{
    [SerializeField]
    [Tooltip("The tools which this item will be able to represent via a HandActivate toggle in-game. Effectively, you'll want this list to be at least 2 entries large.")]
    private List<ItemTrait> toolTraits = null;
    
    private ItemAttributesV2 itemAttributeComponent;
    private int toolSetting = 0;
    
    private void Awake()
    {
        itemAttributeComponent = GetComponent<ItemAttributesV2>();
        
        itemAttributeComponent.AddTrait(toolTraits[toolSetting]);    // send over the first entry in the component's list to the ItemAttributeV2
    }
    
    public void ServerPerformInteraction(HandActivate interaction)
    {
        itemAttributeComponent.RemoveTrait(toolTraits[toolSetting]);    // look over to ItemAttributeV2 and REMOVE the trait specified by the current toolSetting from this component

        // cycle though the list, keeping in check the size of said list
        toolSetting++;
        if (toolSetting >= toolTraits.Count)
        {
            toolSetting = 0;
        }
        
        Chat.AddExamineMsgFromServer(interaction.Performer, $"You flick the {gameObject.name} into {toolTraits[toolSetting].name} mode");
        
        itemAttributeComponent.AddTrait(toolTraits[toolSetting]);    // look over to ItemAttributeV2 and ADD the trait specified by the current toolSetting from this component
    }
}
